# Dark Key Auth Bot

A Discord bot for generating license keys for Dark Optimizer (Basic, Pro, Extreme tiers).

## Setup Instructions

### 1. Configure the Bot

Open `Program.cs` and edit the following:

**Line 110 - Set your Discord Bot Token:**
```csharp
private static readonly string BotToken = "YOUR_BOT_TOKEN_HERE";
```
Replace `YOUR_BOT_TOKEN_HERE` with your actual Discord bot token from the Discord Developer Portal.

**Lines 113-116 - Set Admin/Mod Role IDs:**
```csharp
private static readonly ulong[] AdminRoleIds = { 
    123456789012345678, // Replace with actual Admin role ID
    987654321098765432  // Replace with actual Mod role ID
};
```
To get role IDs:
- Enable Developer Mode in Discord (User Settings > Advanced)
- Right-click on a role in your server
- Copy the ID from the bottom of the menu

### 2. Build the Bot

```bash
cd C:\Users\ryn88\CascadeProjects\DarkOptimizer\DarkKeyAuthBot
dotnet restore
dotnet build --configuration Release
```

### 3. Railway Deployment (24/7 Hosting)

#### Prerequisites
- A Railway account (https://railway.app)
- GitHub account

#### Step-by-Step Deployment

1. **Push to GitHub**
   - Create a new repository on GitHub
   - Push the DarkKeyAuthBot folder to GitHub:
   ```bash
   cd C:\Users\ryn88\CascadeProjects\DarkOptimizer\DarkKeyAuthBot
   git init
   git add .
   git commit -m "Initial commit"
   git branch -M main
   git remote add origin https://github.com/YOUR_USERNAME/YOUR_REPO.git
   git push -u origin main
   ```

2. **Deploy on Railway**
   - Go to https://railway.app and log in
   - Click "New Project" > "Deploy from GitHub repo"
   - Select your repository
   - Railway will automatically detect it's a .NET project

3. **Add Environment Variables**
   - In your Railway project, go to "Variables"
   - Add a new variable:
     - Key: `BOT_TOKEN`
     - Value: Your Discord bot token
   - Add another variable for role IDs:
     - Key: `ADMIN_ROLE_IDS`
     - Value: `123456789012345678,987654321098765432` (comma-separated)

4. **Update Program.cs for Railway**
   - Modify line 110 to read from environment variable:
   ```csharp
   private static readonly string BotToken = Environment.GetEnvironmentVariable("BOT_TOKEN") ?? "YOUR_BOT_TOKEN_HERE";
   ```
   - Modify lines 113-116 to read from environment variable:
   ```csharp
   private static readonly ulong[] AdminRoleIds = (Environment.GetEnvironmentVariable("ADMIN_ROLE_IDS") ?? "123456789012345678,987654321098765432")
       .Split(',')
       .Select(ulong.Parse)
       .ToArray();
   ```

5. **Deploy**
   - Railway will automatically deploy when you push changes
   - Your bot will be online 24/7!

## Bot Commands

### `/generate <tier> <duration> [days]`
Generate a license key (Admin/Mod only)

**Tiers:** Basic, Pro, Extreme  
**Durations:** OneTime, Lifetime, CustomDays

**Examples:**
- `/generate Pro Lifetime`
- `/generate Basic CustomDays 30`
- `/generate Extreme OneTime`

The bot will DM the encrypted license key to the user.

### `/help`
Shows all available commands.

## Important Notes

- Keys start with "DARK-" prefix
- License keys are encrypted and bound to HWID on first use
- OneTime licenses expire after 1 day
- CustomDays licenses expire after specified days
- Lifetime licenses never expire
- Only users with Admin/Mod roles can generate keys
- Keys are sent via DM to keep them private

## Troubleshooting

**Bot not responding:**
- Check that the bot token is correct
- Ensure the bot has "Message Content Intent" enabled in Discord Developer Portal
- Verify role IDs are correct

**Can't send DM:**
- Users must enable "Allow direct messages from server members" in Discord privacy settings
