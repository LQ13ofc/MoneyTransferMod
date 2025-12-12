using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.BellsAndWhistles;
using StardewValley.Menus;

namespace TransferMoneyMod
{
    // =========================================================
    // 1. CONFIGURATION
    // =========================================================
    public class ModConfig
    {
        public KeybindList OpenTransferKey { get; set; } = new(SButton.V);
    }

    // =========================================================
    // 2. DATA MESSAGE
    // =========================================================
    public class TransferMessage
    {
        public long TargetFarmerId { get; set; }
        public int Amount { get; set; }
        public string SenderName { get; set; } = "";
    }

    // =========================================================
    // 3. MAIN CLASS (ModEntry)
    // =========================================================
    public class ModEntry : Mod
    {
        private const string MessageChannel = "TransferMoneyMod.Transaction";
        private ModConfig Config = null!;

        public override void Entry(IModHelper helper)
        {
            Config = helper.ReadConfig<ModConfig>();
            helper.Events.Input.ButtonPressed += OnButtonPressed;
            helper.Events.Multiplayer.ModMessageReceived += OnModMessageReceived;
        }

        private void OnButtonPressed(object? sender, ButtonPressedEventArgs e)
        {
            if (!Context.IsWorldReady || Game1.activeClickableMenu != null) return;

            if (Config.OpenTransferKey.JustPressed())
            {
                this.Helper.Input.SuppressActiveKeybinds(Config.OpenTransferKey);
                Game1.activeClickableMenu = new MoneyTransferMenu(SendMoneyLogic, ToggleMoneySharing);
            }
        }

        private void SendMoneyLogic(Farmer target, int amount)
        {
            if (Game1.player.Money >= amount)
            {
                Game1.player.Money -= amount;
                var message = new TransferMessage { TargetFarmerId = target.UniqueMultiplayerID, Amount = amount, SenderName = Game1.player.Name };
                this.Helper.Multiplayer.SendMessage(message, MessageChannel, modIDs: new[] { this.ModManifest.UniqueID });
                Game1.addHUDMessage(new HUDMessage($"Sent {amount}g to {target.Name}.", 2));
                Game1.playSound("purchase");
            }
            else
            {
                Game1.addHUDMessage(new HUDMessage("Insufficient funds!", 3));
                Game1.playSound("cancel");
            }
        }

        private void ToggleMoneySharing()
        {
            if (!Context.IsMainPlayer)
            {
                Game1.addHUDMessage(new HUDMessage("Only the Host can change this.", 3));
                return;
            }

            var team = Game1.player.team;

            // If wallets are SEPARATE, let's JOIN them (Reshare)
            if (team.useSeparateWallets.Value)
            {
                int totalMoney = 0;
                foreach (var farmer in Game1.getAllFarmers())
                {
                    totalMoney += farmer.Money;
                    team.SetIndividualMoney(farmer, 0); // Reset individual wallets
                }

                team.useSeparateWallets.Value = false;
                Game1.player.Money = totalMoney; // Host's money becomes the shared money
                Game1.addHUDMessage(new HUDMessage($"Money unified! Total: {totalMoney}g", 2));
            }
            // If wallets are JOINED, let's SEPARATE them (Unshare)
            else
            {
                team.useSeparateWallets.Value = true;
                
                // Ensure OTHER players start with 0 in their individual wallets
                var otherFarmers = Game1.getAllFarmers().Where(f => f.UniqueMultiplayerID != Game1.player.UniqueMultiplayerID);
                
                foreach (var farmer in otherFarmers)
                {
                    team.SetIndividualMoney(farmer, 0); 
                }

                Game1.addHUDMessage(new HUDMessage($"Money separated! Host kept {Game1.player.Money}g.", 2));
            }
            Game1.playSound("reward");
        }

        private void OnModMessageReceived(object? sender, ModMessageReceivedEventArgs e)
        {
            if (e.FromModID == this.ModManifest.UniqueID && e.Type == MessageChannel)
            {
                var message = e.ReadAs<TransferMessage>();
                if (message.TargetFarmerId == Game1.player.UniqueMultiplayerID)
                {
                    Game1.player.Money += message.Amount;
                    Game1.addHUDMessage(new HUDMessage($"Received {message.Amount}g from {message.SenderName}!", 2));
                    Game1.playSound("coin");
                }
            }
        }
    }

    // =========================================================
    // 4. MENU CLASS (WITH CYCLIC SELECTOR AND SEND BUTTON)
    // =========================================================
    public class MoneyTransferMenu : IClickableMenu
    {
        private readonly Action<Farmer, int> onSend;
        private readonly Action onToggleSharing;

        private readonly TextBox amountTextBox;
        private readonly ClickableComponent amountTextBoxCC;
        
        // New components for cyclic selection
        private readonly ClickableTextureComponent prevButton;
        private readonly ClickableTextureComponent nextButton;
        private readonly ClickableTextureComponent sendButton;

        private readonly List<Farmer> targetPlayers;
        private int selectedIndex = 0; // Index of selected player
        
        private readonly ClickableTextureComponent? toggleButton;
        private readonly string toggleLabel = "";

        public MoneyTransferMenu(Action<Farmer, int> onSendCallback, Action onToggleSharingCallback)
            : base((int)Utility.getTopLeftPositionForCenteringOnScreen(800, 600).X, (int)Utility.getTopLeftPositionForCenteringOnScreen(800, 600).Y, 800, 600, true)
        {
            this.onSend = onSendCallback;
            this.onToggleSharing = onToggleSharingCallback;

            // Define target players (only if separate)
            if (Game1.player.team.useSeparateWallets.Value)
            {
                this.targetPlayers = Game1.getOnlineFarmers()
                    .Where(p => p.UniqueMultiplayerID != Game1.player.UniqueMultiplayerID)
                    .ToList();
            }
            else
            {
                this.targetPlayers = new List<Farmer>();
            }

            // --- General UI ---
            this.amountTextBox = new TextBox(Game1.content.Load<Texture2D>("LooseSprites\\textBox"), null, Game1.smallFont, Color.Black)
            {
                X = this.xPositionOnScreen + (width / 2) - 96,
                Y = this.yPositionOnScreen + 120,
                Width = 192,
                numbersOnly = true
            };
            this.amountTextBoxCC = new ClickableComponent(new Rectangle(amountTextBox.X, amountTextBox.Y, amountTextBox.Width, 48), "");
            this.amountTextBox.Selected = true;
            
            // --- Selection Buttons (If there are players) ---
            int selectorY = this.yPositionOnScreen + 250;
            int portraitX = this.xPositionOnScreen + (width / 2) - 50;
            
            // "Previous" Button (<)
            this.prevButton = new ClickableTextureComponent(
                "prev",
                new Rectangle(portraitX - 100, selectorY, 48, 44),
                "", "Previous", Game1.mouseCursors, new Rectangle(352, 495, 12, 11), 4f
            );

            // "Next" Button (>)
            this.nextButton = new ClickableTextureComponent(
                "next",
                new Rectangle(portraitX + 100, selectorY, 48, 44),
                "", "Next", Game1.mouseCursors, new Rectangle(365, 495, 12, 11), 4f
            );

            // "SEND" Button (Positioned near the text box)
            this.sendButton = new ClickableTextureComponent(
                "Send", 
                new Rectangle(this.amountTextBox.X + this.amountTextBox.Width + 20, this.amountTextBox.Y, 64, 64), 
                null, "Send Gold", Game1.mouseCursors, 
                new Rectangle(304, 495, 12, 12), 4f
            );

            // --- Host Button ---
            if (Context.IsMainPlayer)
            {
                bool isSeparate = Game1.player.team.useSeparateWallets.Value;
                this.toggleLabel = isSeparate ? "Merge Money" : "Separate Money";
                
                this.toggleButton = new ClickableTextureComponent(
                    "toggle",
                    new Rectangle(this.xPositionOnScreen + (width / 2) - 32, this.yPositionOnScreen + 450, 64, 64),
                    "",
                    this.toggleLabel,
                    Game1.mouseCursors,
                    new Rectangle(293, 360, 24, 24), 
                    2.5f
                );
            }
        }

        public override void receiveLeftClick(int x, int y, bool playSound = true)
        {
            base.receiveLeftClick(x, y, playSound);
            
            if (amountTextBoxCC.containsPoint(x, y)) amountTextBox.SelectMe();
            else amountTextBox.Selected = false;

            // Host button logic
            if (toggleButton != null && toggleButton.containsPoint(x, y))
            {
                onToggleSharing();
                exitThisMenu();
                return;
            }

            // Selection and sending logic (only if separate wallets and players exist)
            if (Game1.player.team.useSeparateWallets.Value && targetPlayers.Count > 0)
            {
                // Cycle buttons
                if (prevButton.containsPoint(x, y))
                {
                    selectedIndex = (selectedIndex - 1 + targetPlayers.Count) % targetPlayers.Count;
                    Game1.playSound("shwip");
                }
                else if (nextButton.containsPoint(x, y))
                {
                    selectedIndex = (selectedIndex + 1) % targetPlayers.Count;
                    Game1.playSound("shwip");
                }
                
                // SEND Button
                else if (sendButton.containsPoint(x, y))
                {
                    if (int.TryParse(amountTextBox.Text, out int amt) && amt > 0)
                    {
                        Farmer target = targetPlayers[selectedIndex];
                        onSend(target, amt);
                        exitThisMenu();
                    }
                }
            }
        }

        public override void receiveKeyPress(Keys key) { if (key == Keys.Escape || key == Keys.V) exitThisMenu(); base.receiveKeyPress(key); }
        
        public override void performHoverAction(int x, int y) 
        { 
            base.performHoverAction(x, y); 
            if (toggleButton != null) toggleButton.scale = toggleButton.containsPoint(x, y) ? 2.6f : 2.5f;

            if (Game1.player.team.useSeparateWallets.Value && targetPlayers.Count > 0)
            {
                prevButton.scale = prevButton.containsPoint(x, y) ? 4.2f : 4f;
                nextButton.scale = nextButton.containsPoint(x, y) ? 4.2f : 4f;
                sendButton.scale = sendButton.containsPoint(x, y) ? 4.2f : 4f;
            }
        }

        public override void draw(SpriteBatch b)
        {
            b.Draw(Game1.fadeToBlackRect, Game1.graphics.GraphicsDevice.Viewport.Bounds, Color.Black * 0.75f);
            drawTextureBox(b, xPositionOnScreen, yPositionOnScreen, width, height, Color.White);

            bool isSeparate = Game1.player.team.useSeparateWallets.Value;
            string title = isSeparate ? "Transfer" : "Shared Money";

            SpriteText.drawStringHorizontallyCenteredAt(b, title, xPositionOnScreen + width / 2, yPositionOnScreen + 35);
            Utility.drawTextWithShadow(b, $"Your Balance: {Game1.player.Money}g", Game1.dialogueFont, new Vector2(xPositionOnScreen + 50, yPositionOnScreen + 80), Color.Gold);

            if (isSeparate)
            {
                amountTextBox.Draw(b);
                sendButton.draw(b);
                
                // Draws cyclic selection
                if (targetPlayers.Count > 0)
                {
                    Farmer selectedTarget = targetPlayers[selectedIndex];

                    prevButton.draw(b);
                    nextButton.draw(b);

                    // Draws the portrait of the selected player
                    Vector2 portraitPos = new Vector2(this.xPositionOnScreen + (width / 2) - 50, this.yPositionOnScreen + 235);
                    selectedTarget.FarmerRenderer.drawMiniPortrat(b, portraitPos, 0.8f, 4f, 0, selectedTarget);
                    
                    // Draws the name
                    Utility.drawTextWithShadow(b, selectedTarget.Name, Game1.smallFont, 
                        new Vector2(this.xPositionOnScreen + width / 2 - Game1.smallFont.MeasureString(selectedTarget.Name).X / 2, 
                                    this.yPositionOnScreen + 320), 
                        Game1.textColor);
                        
                    // Instruction
                    Utility.drawTextWithShadow(b, "Target Player:", Game1.smallFont, new Vector2(xPositionOnScreen + 50, yPositionOnScreen + 200), Game1.textColor);
                }
                else
                {
                    Utility.drawTextWithShadow(b, "No other players online to send to.", Game1.smallFont, new Vector2(xPositionOnScreen + 50, yPositionOnScreen + 200), Game1.textColor);
                }
            }
            else
            {
                string msg = "Money is shared (everyone has the same).";
                Utility.drawTextWithShadow(b, msg, Game1.smallFont, new Vector2(xPositionOnScreen + 50, yPositionOnScreen + 200), Game1.textColor);
            }

            // Draws the Host button if it exists
            if (toggleButton != null)
            {
                toggleButton.draw(b);
                Vector2 textSize = Game1.smallFont.MeasureString(toggleLabel);
                Utility.drawTextWithShadow(b, toggleLabel, Game1.smallFont, 
                    new Vector2(toggleButton.bounds.X + (toggleButton.bounds.Width/2) - (textSize.X/2), toggleButton.bounds.Y + 65), 
                    Color.Red);
            }

            drawMouse(b);
        }
    }
}