# Personal Media App

A family-friendly social media app that displays AI-generated images in a scrolling feed with like/dislike functionality.

## Project Structure

- **PersonalMedia.Web** - ASP.NET Core web application
- **PersonalMedia.Functions** - Azure Function for nightly image generation (runs at 3 AM)
- **PersonalMedia.Core** - Domain entities
- **PersonalMedia.Data** - EF Core DbContext and migrations
- **PersonalMedia.Services** - Business logic services (Azure Storage, Image Generation)

## Features Implemented

### Web Application
- **Simple Authentication**: Code-based sign-in (default code: 1234)
- **Image Feed**: Vertical scrolling of image sets
- **Horizontal Carousels**: Each set contains 3-5 images that scroll sideways
- **Like/Dislike Buttons**: React to images
- **Pinch-to-Zoom**: Full-screen modal with touch zoom support
- **Responsive Design**: Works on desktop and mobile

### Database Schema
- **MediaSet**: Groups of images (5 sets per day)
- **MediaItem**: Individual images/videos with generation status tracking
- **MediaReaction**: User likes/dislikes
- **BasePersonImage**: Source person images for AI generation
- **ParameterOption**: Categories like setting, mood, activity, clothing, etc.
- **GenerationSettings**: Configuration for daily generation

### Azure Function
- **Timer Trigger**: Runs daily at 3 AM
- **Image Generation**: Creates 5 sets of 3-5 images each
- **Retry Logic**: Handles failures with configurable max retry attempts
- **Prompt Generation**: Random selection from parameter categories
- **Azure Storage Upload**: Saves generated images to blob storage

## Getting Started

### 1. Database Setup

Create the database using EF Core migrations:

```bash
cd PersonalMedia.Web
dotnet ef database update --project ../PersonalMedia.Data/PersonalMedia.Data.csproj
```

### 2. Configuration

#### Web Application (appsettings.json)
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Your SQL Server connection string"
  },
  "AppSettings": {
    "AccessCode": "1234"
  },
  "AzureStorage": {
    "ConnectionString": "Your Azure Storage connection string",
    "ContainerName": "personal-media"
  },
  "ImageGeneration": {
    "ApiKey": "Your OpenAI or other AI service API key",
    "Endpoint": "https://api.openai.com/v1/images/generations"
  }
}
```

#### Azure Function (local.settings.json)
```json
{
  "Values": {
    "AzureWebJobsStorage": "Your Azure Storage connection string",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated",
    "ConnectionStrings:DefaultConnection": "Your SQL Server connection string",
    "AzureStorage:ConnectionString": "Your Azure Storage connection string",
    "AzureStorage:ContainerName": "personal-media",
    "ImageGeneration:ApiKey": "Your API key",
    "ImageGeneration:Endpoint": "https://api.openai.com/v1/images/generations"
  }
}
```

### 3. Run the Application

#### Web App
```bash
cd PersonalMedia.Web
dotnet run
```

Navigate to: https://localhost:5001

#### Azure Function (Local)
```bash
cd PersonalMedia.Functions
func start
```

Or run directly:
```bash
cd PersonalMedia.Functions
dotnet run
```

## Usage

1. **Sign In**: Enter the access code (default: 1234)
2. **View Feed**: Scroll through image sets vertically
3. **Navigate Images**: Swipe left/right or use arrow buttons for images in a set
4. **React**: Click heart to like, X to dislike
5. **Full View**: Click any image to view full screen with pinch-to-zoom

## Data Management

### Add Base Person Images
Insert base person images into the database:

```sql
INSERT INTO BasePersonImages (Name, AzureStorageUrl, IsActive, CreatedDate, UsageCount)
VALUES ('Person 1', 'https://your-storage.blob.core.windows.net/personal-media/base/person1.jpg', 1, GETUTCDATE(), 0);
```

### Configure Parameters
Parameter options are seeded automatically during migration. You can modify them:

```sql
-- Add new setting
INSERT INTO ParameterOptions (Category, Value, IsActive, Weight)
VALUES (1, 'Desert', 1, 1);

-- Disable a parameter
UPDATE ParameterOptions SET IsActive = 0 WHERE Value = 'Beach';
```

### Adjust Generation Settings
```sql
UPDATE GenerationSettings
SET DailySetsCount = 10,
    ImagesPerSetMin = 5,
    ImagesPerSetMax = 7,
    ModestyLevel = 'Very Conservative';
```

## Future Enhancements

- Video support (schema already supports it)
- User preferences based on likes/dislikes
- Advanced AI prompting based on reaction history
- Multiple user support
- Favorites collection
- Social sharing

## API Endpoints

- `POST /api/media/react` - Toggle like/dislike on an image
  ```json
  {
    "mediaItemId": 1,
    "reactionType": "like"
  }
  ```

## Timer Schedule

The Azure Function runs on a cron schedule:
- `"0 0 3 * * *"` = Every day at 3:00 AM

To change the schedule, modify the `TimerTrigger` attribute in [ImageGenerationFunction.cs](PersonalMedia.Functions/Functions/ImageGenerationFunction.cs:33).

## Troubleshooting

### Images not generating
1. Check Azure Function logs
2. Verify API key is correct
3. Check `GenerationStatus` in MediaItems table
4. Review error messages in `MediaItems.ErrorMessage`

### Database connection issues
- Ensure SQL Server is running
- Verify connection string
- Check migrations are applied

### Azure Storage issues
- Ensure container exists
- Verify connection string
- Check container permissions (allow public blob access)

## Technologies Used

- ASP.NET Core 10.0
- Entity Framework Core 10.0
- Azure Functions v4
- Azure Blob Storage
- Bootstrap 5
- WebOptimizer
- SQL Server
