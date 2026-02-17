$filePath = "Z:\repos\chronicis\src\Chronicis.Client\Components\Articles\ArticleDetail.razor"
$content = Get-Content $filePath -Raw

# Replace all patterns
$content = $content -creplace '\b_article\.', 'ViewModel.Article.'
$content = $content -creplace '\b_article\b', 'ViewModel.Article'
$content = $content -creplace '\b_editTitle\b', 'EditTitle'
$content = $content -creplace '\b_editBody\b', 'EditBody'
$content = $content -creplace '\b_isSaving\b', 'ViewModel.IsSaving'
$content = $content -creplace '\b_openMetadata\b', 'ShowMetadataDrawer'
$content = $content -replace 'ArticleApi\.CreateArticleAsync', 'ViewModel.CreateArticleAsync'
$content = $content -replace 'ArticleApi\.UpdateArticleAsync', 'ViewModel.UpdateArticleAsync'
$content = $content -replace 'ArticleApi\.GetArticleDetailAsync', 'ViewModel.GetArticleDetailAsync'
$content = $content -replace 'await ArticleCache\.GetNavigationPathAsync', 'await ViewModel.GetNavigationPathAsync'
$content = $content -replace 'await ArticleCache\.GetArticlePathAsync', 'await ViewModel.GetArticlePathAsync'

# Write back
$content | Set-Content -Path $filePath -NoNewline
Write-Host "✅ Fixed ArticleDetail.razor"
