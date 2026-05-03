using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using System;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace DarkKeyAuthBot
{
    public enum LicenseTier
    {
        Basic,
        Pro,
        Extreme
    }

    public enum LicenseDuration
    {
        OneTime,
        Lifetime,
        CustomDays
    }

    public class LicenseData
    {
        public string Key { get; set; } = string.Empty;
        public string HWID { get; set; } = string.Empty;
        public LicenseTier Tier { get; set; }
        public LicenseDuration Duration { get; set; }
        public int CustomDays { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public bool IsActive { get; set; }
        public bool IsRevoked { get; set; }
    }

    public class LicenseManager
    {
        private static readonly string SecretKey = "DARK_OPTIMIZER_SECRET_KEY_2024";
        
        public static string GenerateLicenseKey()
        {
            using (var rng = RandomNumberGenerator.Create())
            {
                byte[] bytes = new byte[16];
                rng.GetBytes(bytes);
                return "DARK-" + Convert.ToHexString(bytes).Substring(0, 24);
            }
        }

        public static string EncryptLicense(LicenseData license)
        {
            string json = JsonSerializer.Serialize(license);
            byte[] key = Encoding.UTF8.GetBytes(SecretKey.PadRight(32).Substring(0, 32));
            byte[] iv = Encoding.UTF8.GetBytes(SecretKey.Substring(0, 16));
            
            using (var aes = Aes.Create())
            {
                aes.Key = key;
                aes.IV = iv;
                
                using (var encryptor = aes.CreateEncryptor())
                using (var ms = new System.IO.MemoryStream())
                {
                    using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                    using (var sw = new System.IO.StreamWriter(cs))
                    {
                        sw.Write(json);
                    }
                    return Convert.ToBase64String(ms.ToArray());
                }
            }
        }

        public static LicenseData CreateLicense(LicenseTier tier, LicenseDuration duration, int customDays = 0)
        {
            var license = new LicenseData
            {
                Key = GenerateLicenseKey(),
                HWID = string.Empty,
                Tier = tier,
                Duration = duration,
                CustomDays = customDays,
                CreatedDate = DateTime.Now,
                IsActive = true,
                IsRevoked = false
            };
            
            if (duration == LicenseDuration.CustomDays)
            {
                license.ExpiryDate = DateTime.Now.AddDays(customDays);
            }
            else if (duration == LicenseDuration.OneTime)
            {
                license.ExpiryDate = DateTime.Now.AddDays(1);
            }
            
            return license;
        }
    }

    public class Program
    {
        private static DiscordSocketClient? _client;
        private static InteractionService? _interactions;
        
        // PASTE YOUR BOT TOKEN HERE (or use BOT_TOKEN environment variable for Railway)
        private static readonly string BotToken = Environment.GetEnvironmentVariable("BOT_TOKEN") ?? "MTUwMDUwODQzNjM0NDA4MjQzMg.G0ffEg.YYCNgYaJDvOb81w2LvYXEAcu006IxiNVmt_4Ac";
        
        // Allowed role IDs (replace with your actual role IDs or use ADMIN_ROLE_IDS environment variable)
        private static readonly ulong[] AdminRoleIds = (Environment.GetEnvironmentVariable("ADMIN_ROLE_IDS") ?? "1500503947763519609")
            .Split(',')
            .Select(ulong.Parse)
            .ToArray();

        public static async Task Main(string[] args)
        {
            if (BotToken == "YOUR_BOT_TOKEN_HERE")
            {
                Console.WriteLine("ERROR: Please set your bot token in the code!");
                Console.ReadLine();
                return;
            }

            _client = new DiscordSocketClient(new DiscordSocketConfig
            {
                GatewayIntents = GatewayIntents.Guilds | GatewayIntents.GuildMessages | GatewayIntents.DirectMessages
            });

            _interactions = new InteractionService(_client.Rest);

            _client.Log += LogAsync;
            _interactions.Log += LogAsync;

            _client.Ready += async () =>
            {
                await _interactions.AddModuleAsync<LicenseCommands>(null);
                await _interactions.RegisterCommandsGloballyAsync();
                Console.WriteLine("Bot is ready! Slash commands registered.");
            };

            _client.InteractionCreated += async interaction =>
            {
                var ctx = new SocketInteractionContext(_client, interaction);
                await _interactions.ExecuteCommandAsync(ctx);
            };

            await _client.LoginAsync(TokenType.Bot, BotToken);
            await _client.StartAsync();

            // Block the program until it is closed
            await Task.Delay(-1);
        }

        private static Task LogAsync(LogMessage message)
        {
            Console.WriteLine(message.ToString());
            return Task.CompletedTask;
        }

        public static bool HasAdminRole(SocketGuildUser user)
        {
            foreach (var roleId in AdminRoleIds)
            {
                if (user.Roles.Any(r => r.Id == roleId))
                    return true;
            }
            return false;
        }
    }

    public class LicenseCommands : InteractionModuleBase<SocketInteractionContext>
    {
        [SlashCommand("generate", "Generate a license key for a user")]
        public async Task GenerateLicenseAsync(
            [Summary("The user to receive the license")] IUser targetUser,
            [Summary("Product tier: Basic, Pro, or Extreme")] string product,
            [Summary("Duration: OneTime, Lifetime, or CustomDays")] string duration,
            [Summary("Number of days (only for CustomDays)")] int days = 0)
        {
            var user = Context.User as SocketGuildUser;
            if (user == null)
            {
                await RespondAsync("This command can only be used in a server.", ephemeral: true);
                return;
            }

            if (!Program.HasAdminRole(user))
            {
                await RespondAsync("❌ You don't have permission to generate keys. This command is for admins and mods only.", ephemeral: true);
                return;
            }

            // Parse product (tier)
            if (!Enum.TryParse<LicenseTier>(product, true, out var licenseTier))
            {
                await RespondAsync("❌ Invalid product. Use: Basic, Pro, or Extreme", ephemeral: true);
                return;
            }

            // Parse duration
            if (!Enum.TryParse<LicenseDuration>(duration, true, out var licenseDuration))
            {
                await RespondAsync("❌ Invalid duration. Use: OneTime, Lifetime, or CustomDays", ephemeral: true);
                return;
            }

            // Validate custom days
            if (licenseDuration == LicenseDuration.CustomDays && days <= 0)
            {
                await RespondAsync("❌ Please specify the number of days for CustomDays duration.", ephemeral: true);
                return;
            }

            // Generate license
            var license = LicenseManager.CreateLicense(licenseTier, licenseDuration, days);
            string encryptedLicense = LicenseManager.EncryptLicense(license);

            // DM the key to the target user
            try
            {
                var dmChannel = await targetUser.CreateDMChannelAsync();
                await dmChannel.SendMessageAsync($"🔑 **Your License Key**\n\n" +
                    $"**Product:** {licenseTier}\n" +
                    $"**Duration:** {licenseDuration}" + (licenseDuration == LicenseDuration.CustomDays ? $" ({days} days)" : "") + "\n" +
                    $"**Key:** `{encryptedLicense}`\n\n" +
                    $"Copy this key and use it in the Dark Optimizer.");
                
                await RespondAsync($"✅ License key sent to {targetUser.Mention} via DM!");
            }
            catch
            {
                await RespondAsync("❌ Could not send DM. Please enable DMs from server members.", ephemeral: true);
            }
        }

        [SlashCommand("help", "Show bot commands")]
        public async Task HelpAsync()
        {
            var embed = new Discord.EmbedBuilder()
                .WithTitle("🔐 Dark Key Auth Bot Commands")
                .WithColor(Discord.Color.Blue)
                .AddField("/generate", 
                    "Generate a license key for a user.\n" +
                    "Products: Basic, Pro, Extreme\n" +
                    "Durations: OneTime, Lifetime, CustomDays\n" +
                    "Example: /generate @User Pro Lifetime\n" +
                    "Example: /generate @User Basic CustomDays 30")
                .WithFooter("Only admins and mods can generate keys")
                .Build();

            await RespondAsync(embed: embed, ephemeral: true);
        }
    }
}
