# PowerShell script to seed Elasticsearch with sample game data

$elasticUrl = "http://localhost:9200"
$indexName = "games"

Write-Host "Seeding Elasticsearch with sample game data..." -ForegroundColor Green
Write-Host ""

# Sample games data
$games = @(
    @{
        id = "1"
        title = "The Legend of Zelda: Breath of the Wild"
        description = "An open-world action-adventure game set in the kingdom of Hyrule"
        genre = "Action-Adventure"
        publisher = "Nintendo"
        releaseDate = "2017-03-03"
        price = 59.99
        rating = 9.7
    },
    @{
        id = "2"
        title = "God of War"
        description = "Follow Kratos and his son Atreus on a journey through Norse mythology"
        genre = "Action-Adventure"
        publisher = "Sony Interactive Entertainment"
        releaseDate = "2018-04-20"
        price = 49.99
        rating = 9.5
    },
    @{
        id = "3"
        title = "Minecraft"
        description = "A sandbox game where you can build and explore infinite worlds"
        genre = "Sandbox"
        publisher = "Mojang Studios"
        releaseDate = "2011-11-18"
        price = 26.95
        rating = 9.0
    },
    @{
        id = "4"
        title = "Red Dead Redemption 2"
        description = "An epic tale of life in America's unforgiving heartland"
        genre = "Action-Adventure"
        publisher = "Rockstar Games"
        releaseDate = "2018-10-26"
        price = 59.99
        rating = 9.8
    },
    @{
        id = "5"
        title = "The Witcher 3: Wild Hunt"
        description = "A story-driven open world RPG set in a visually stunning fantasy universe"
        genre = "RPG"
        publisher = "CD Projekt Red"
        releaseDate = "2015-05-19"
        price = 39.99
        rating = 9.6
    },
    @{
        id = "6"
        title = "Elden Ring"
        description = "A fantasy action-RPG adventure set within a world created by Hidetaka Miyazaki and George R.R. Martin"
        genre = "Action-RPG"
        publisher = "FromSoftware"
        releaseDate = "2022-02-25"
        price = 59.99
        rating = 9.4
    },
    @{
        id = "7"
        title = "Hades"
        description = "A rogue-like dungeon crawler where you defy the god of the dead"
        genre = "Rogue-like"
        publisher = "Supergiant Games"
        releaseDate = "2020-09-17"
        price = 24.99
        rating = 9.3
    },
    @{
        id = "8"
        title = "Portal 2"
        description = "A first-person puzzle-platform game with mind-bending challenges"
        genre = "Puzzle"
        publisher = "Valve"
        releaseDate = "2011-04-19"
        price = 19.99
        rating = 9.5
    }
)

$successCount = 0
$errorCount = 0

foreach ($game in $games) {
    try {
        $json = $game | ConvertTo-Json -Compress
        $url = "$elasticUrl/$indexName/_doc/$($game.id)"
        
        $response = Invoke-RestMethod -Uri $url -Method Put -Body $json -ContentType "application/json"
        
        Write-Host "✓ Indexed: " -NoNewline -ForegroundColor Green
        Write-Host $game.title -ForegroundColor White
        $successCount++
    }
    catch {
        Write-Host "✗ Failed: " -NoNewline -ForegroundColor Red
        Write-Host "$($game.title) - $($_.Exception.Message)" -ForegroundColor Yellow
        $errorCount++
    }
}

Write-Host ""
Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Cyan
Write-Host "Seeding complete!" -ForegroundColor Green
Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Cyan
Write-Host "Success: $successCount | Errors: $errorCount" -ForegroundColor White
Write-Host ""
Write-Host "You can now search for games using the API:" -ForegroundColor Yellow
Write-Host "  Example: /api/games/search?query=zelda" -ForegroundColor Cyan
Write-Host ""
Write-Host "Or view in Kibana: " -NoNewline
Write-Host "http://localhost:5601" -ForegroundColor Cyan
Write-Host ""

