# Fix ArticleDetail.razor - Replace all old references with ViewModel references
$filePath = "Z:\repos\chronicis\src\Chronicis.Client\Components\Articles\ArticleDetail.razor"
$content = Get-Content $filePath -Raw

# Replace all _article references with ViewModel.Article
$content = $content -creplace '(?<![a-zA-Z_])_article\.', 'ViewModel.Article.'
$content = $content -creplace '\b_article\b', 'ViewModel.Article'

# Replace _editTitle and _editBody with property versions
$content = $content -creplace '\b_editTitle\b', 'EditTitle'
$content = $content -creplace '\b_editBody\b', 'EditBody'

# Replace _isSaving and _isLoading with ViewModel versions
$content = $content -creplace '\b_isSaving\b', 'ViewModel.IsSaving'

# Replace _openMetadata with ShowMetadataDrawer property
$content = $content -creplace '\b_openMetadata\b', 'ShowMetadataDrawer'

# Replace direct service calls that should go through ViewModel/Facade
$content = $content -creplace 'await ArticleApi\.CreateArticleAsync', 'await ViewModel.CreateArticleAsync'
$content = $content -creplace 'await ArticleApi\.UpdateArticleAsync', 'await ViewModel.UpdateArticleAsync'
$content = $content -creplace 'await ArticleApi\.GetArticleDetailAsync', 'await ViewModel.GetArticleDetailAsync'
$content = $content -creplace 'await ArticleCache\.GetNavigationPathAsync', 'await ViewModel.GetNavigationPathAsync'
$content = $content -creplace 'await ArticleCache\.GetArticlePathAsync', 'await ViewModel.GetArticlePathAsync'
$content = $content -creplace 'BreadcrumbService\.BuildArticleUrl', 'string.Join("/", '

# Save the file
Set-Content -Path $filePath -Value $content -NoNewline
Write-Host "Fixed ArticleDetail.razor"
