# Fix method names to match Facade interface
$filePath = "Z:\repos\chronicis\src\Chronicis.Client\Components\Articles\ArticleDetail.razor"
$text = [System.IO.File]::ReadAllText($filePath)

# Fix method names
$text = $text -replace 'await Facade\.GetArticleDetailAsync', 'await Facade.GetArticleAsync'
$text = $text -replace 'await Facade\.GetNavigationPathAsync', 'await Facade.GetArticleNavigationPathAsync'
$text = $text -replace 'await Facade\.GetArticlePathAsync', 'await Facade.GetArticleNavigationPathAsync'

[System.IO.File]::WriteAllText($filePath, $text)
Write-Host "Fixed facade method names"
