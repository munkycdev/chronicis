# Test script for ROS provider
# This script helps diagnose issues with the ROS external resource provider

$apiBaseUrl = 'http://localhost:7071'

Write-Host 'Testing ROS Provider Integration' -ForegroundColor Cyan
Write-Host '=================================' -ForegroundColor Cyan
Write-Host ''

# Test 1: Check if API is running
Write-Host '1. Checking API availability...' -ForegroundColor Yellow
try {
    $response = Invoke-WebRequest -Uri "$apiBaseUrl/api/health" -Method GET -ErrorAction SilentlyContinue
    Write-Host '   ✓ API is running' -ForegroundColor Green
} catch {
    Write-Host '   ✗ API is not responding. Please start the API first.' -ForegroundColor Red
    exit 1
}

Write-Host ''

# Test 2: Search for ROS categories (empty query should return all categories)
Write-Host '2. Fetching ROS categories...' -ForegroundColor Yellow
try {
    $uri = '{0}/api/externallinks/search?source=ros&query=' -f $apiBaseUrl
    $response = Invoke-RestMethod -Uri $uri -Method GET
    if ($response.Count -gt 0) {
        Write-Host "   ✓ Found $($response.Count) categories" -ForegroundColor Green
        $response | ForEach-Object { 
            Write-Host "     - $($_.Title) (ID: $($_.Id))" -ForegroundColor Gray 
        }
    } else {
        Write-Host '   ✗ No categories found. Check blob storage structure.' -ForegroundColor Red
    }
} catch {
    Write-Host "   ✗ Error fetching categories: $_" -ForegroundColor Red
}

Write-Host ''

# Test 3: Try searching within a category (if we found any)
if ($response.Count -gt 0) {
    $firstCategory = $response[0]
    # Extract category name from _category/name format
    $categoryName = $firstCategory.Id -replace '^_category/', ''
    
    Write-Host "3. Searching in category '$categoryName'..." -ForegroundColor Yellow
    try {
        $uri = '{0}/api/externallinks/search?source=ros&query={1}/' -f $apiBaseUrl, $categoryName
        $searchResponse = Invoke-RestMethod -Uri $uri -Method GET
        if ($searchResponse.Count -gt 0) {
            Write-Host "   ✓ Found $($searchResponse.Count) items in category" -ForegroundColor Green
            $searchResponse | Select-Object -First 5 | ForEach-Object {
                Write-Host "     - $($_.Title) (ID: $($_.Id))" -ForegroundColor Gray
            }
            
            # Test 4: Try to fetch content for first item
            if ($searchResponse.Count -gt 0) {
                Write-Host ''
                $firstItem = $searchResponse[0]
                Write-Host "4. Fetching content for '$($firstItem.Title)'..." -ForegroundColor Yellow
                Write-Host "   ID: $($firstItem.Id)" -ForegroundColor Gray
                
                try {
                    $encodedId = [System.Web.HttpUtility]::UrlEncode($firstItem.Id)
                    $uri = '{0}/api/externallinks/content?source=ros&id={1}' -f $apiBaseUrl, $encodedId
                    $contentResponse = Invoke-RestMethod -Uri $uri -Method GET
                    
                    if ($contentResponse.Title -ne 'Content Not Found') {
                        Write-Host '   ✓ Content retrieved successfully' -ForegroundColor Green
                        Write-Host "     Title: $($contentResponse.Title)" -ForegroundColor Gray
                        Write-Host "     Kind: $($contentResponse.Kind)" -ForegroundColor Gray
                        Write-Host "     Markdown length: $($contentResponse.Markdown.Length) chars" -ForegroundColor Gray
                    } else {
                        Write-Host '   ✗ Content not found. Check logs for details.' -ForegroundColor Red
                        Write-Host "     Markdown: $($contentResponse.Markdown)" -ForegroundColor Gray
                    }
                } catch {
                    Write-Host "   ✗ Error fetching content: $_" -ForegroundColor Red
                }
            }
        } else {
            Write-Host '   ! Category is empty' -ForegroundColor Yellow
        }
    } catch {
        Write-Host "   ✗ Error searching category: $_" -ForegroundColor Red
    }
}

Write-Host ''
Write-Host 'Test complete. Check API logs for detailed error messages.' -ForegroundColor Cyan
