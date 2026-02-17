# Fix remaining method calls - use Facade instead of ViewModel
$filePath = "Z:\repos\chronicis\src\Chronicis.Client\Components\Articles\ArticleDetail.razor"
$text = [System.IO.File]::ReadAllText($filePath)

# These methods are on the Facade, not ViewModel
$text = $text -replace 'await ViewModel\.UpdateArticleAsync', 'await ViewModel.Facade.UpdateArticleAsync'
$text = $text -replace 'await ViewModel\.CreateArticleAsync', 'await ViewModel.Facade.CreateArticleAsync'
$text = $text -replace 'await ViewModel\.GetArticleDetailAsync', 'await ViewModel.Facade.GetArticleDetailAsync'
$text = $text -replace 'await ViewModel\.GetNavigationPathAsync', 'await ViewModel.Facade.GetNavigationPathAsync'
$text = $text -replace 'await ViewModel\.GetArticlePathAsync', 'await ViewModel.Facade.GetArticlePathAsync'

[System.IO.File]::WriteAllText($filePath, $text)
Write-Host "Fixed facade method calls"
