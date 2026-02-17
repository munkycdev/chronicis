# Fix method calls to use Facade instead of ViewModel.Facade
$filePath = "Z:\repos\chronicis\src\Chronicis.Client\Components\Articles\ArticleDetail.razor"
$text = [System.IO.File]::ReadAllText($filePath)

# Use Facade directly
$text = $text -replace 'await ViewModel\.Facade\.UpdateArticleAsync', 'await Facade.UpdateArticleAsync'
$text = $text -replace 'await ViewModel\.Facade\.CreateArticleAsync', 'await Facade.CreateArticleAsync'
$text = $text -replace 'await ViewModel\.Facade\.GetArticleDetailAsync', 'await Facade.GetArticleDetailAsync'
$text = $text -replace 'await ViewModel\.Facade\.GetNavigationPathAsync', 'await Facade.GetNavigationPathAsync'
$text = $text -replace 'await ViewModel\.Facade\.GetArticlePathAsync', 'await Facade.GetArticlePathAsync'

[System.IO.File]::WriteAllText($filePath, $text)
Write-Host "Fixed to use Facade directly"
