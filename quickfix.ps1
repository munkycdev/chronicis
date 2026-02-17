# Quick fix script that reads, replaces, and writes in one go
$filePath = "Z:\repos\chronicis\src\Chronicis.Client\Components\Articles\ArticleDetail.razor"

# Read entire file
$text = [System.IO.File]::ReadAllText($filePath)

# Do all replacements
$text = $text -creplace '\b_article\.', 'ViewModel.Article.'
$text = $text -creplace '\b_article\b', 'ViewModel.Article'
$text = $text -creplace '\b_editTitle\b', 'EditTitle'
$text = $text -creplace '\b_editBody\b', 'EditBody'
$text = $text -creplace '\b_isSaving\b', 'ViewModel.IsSaving'
$text = $text -creplace '\b_openMetadata\b', 'ShowMetadataDrawer'
$text = $text -replace 'ArticleApi\.CreateArticleAsync', 'ViewModel.CreateArticleAsync'
$text = $text -replace 'ArticleApi\.UpdateArticleAsync', 'ViewModel.UpdateArticleAsync'
$text = $text -replace 'ArticleApi\.GetArticleDetailAsync', 'ViewModel.GetArticleDetailAsync'
$text = $text -replace 'await ArticleCache\.GetNavigationPathAsync', 'await ViewModel.GetNavigationPathAsync'
$text = $text -replace 'await ArticleCache\.GetArticlePathAsync', 'await ViewModel.GetArticlePathAsync'

# Write back in one shot
[System.IO.File]::WriteAllText($filePath, $text)

Write-Host "Done!"
