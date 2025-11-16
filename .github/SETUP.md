# GitHub Actions Setup Guide

This repository includes automated CI/CD workflows for building and publishing to NuGet.

## Workflows

### 1. CI Build (`ci.yml`)
- **Triggers**: On every push to `master`, `main`, or `develop` branches, and on pull requests
- **Purpose**: Validates that the code builds successfully
- **Actions**:
  - Restores dependencies
  - Builds the solution
  - Runs tests (if available)
  - Creates a package (validation only, not published)

### 2. Publish to NuGet (`publish-nuget.yml`)
- **Triggers**:
  - Automatically on version tags (e.g., `v1.0.0`)
  - Manually via GitHub Actions tab
- **Purpose**: Builds and publishes the NuGet package
- **Actions**:
  - Builds the package with specified version
  - Publishes to NuGet.org
  - Creates a GitHub Release with the package

## Setup Instructions

### 1. Create NuGet API Key

1. Go to https://www.nuget.org/account/apikeys
2. Click "Create"
3. Configure:
   - **Key Name**: `BlazorFlow GitHub Actions`
   - **Select Scopes**: Check "Push"
   - **Select Packages**:
     - Choose "Glob Pattern"
     - Enter: `BlazorFlow*`
4. Click "Create"
5. **Copy the API key immediately** (you won't see it again!)

### 2. Add Secret to GitHub

1. Go to your repository on GitHub
2. Navigate to **Settings** → **Secrets and variables** → **Actions**
3. Click **New repository secret**
4. Add:
   - **Name**: `NUGET_API_KEY`
   - **Secret**: Paste your NuGet API key
5. Click **Add secret**

### 3. Usage

#### Automated Release (Recommended)

When you're ready to publish a new version:

```bash
# Update version in BlazorFlow.csproj if needed (optional - workflow will do it)
# Then create and push a version tag
git tag v1.0.0
git push origin v1.0.0
```

The workflow will:
- Build the package with version `1.0.0`
- Publish to NuGet.org
- Create a GitHub Release

#### Manual Release

1. Go to **Actions** tab in GitHub
2. Select "Publish to NuGet" workflow
3. Click "Run workflow"
4. Enter the version number (e.g., `1.0.1`)
5. Click "Run workflow"

## Version Tagging Best Practices

### Semantic Versioning

Use [semantic versioning](https://semver.org/): `MAJOR.MINOR.PATCH`

- **MAJOR**: Breaking changes (e.g., `v2.0.0`)
- **MINOR**: New features, backwards compatible (e.g., `v1.1.0`)
- **PATCH**: Bug fixes (e.g., `v1.0.1`)

### Pre-release Versions

For beta/alpha releases:

```bash
git tag v1.0.0-beta.1
git push origin v1.0.0-beta.1
```

The workflow will mark it as a pre-release on GitHub.

### Deleting Tags

If you need to delete a tag:

```bash
# Delete locally
git tag -d v1.0.0

# Delete on remote
git push origin :refs/tags/v1.0.0
```

## Monitoring Builds

- **CI Builds**: Check the "Actions" tab to see build status for each push/PR
- **Package Status**: Visit https://www.nuget.org/packages/BlazorFlow to see published versions
- **Releases**: Check the "Releases" section in your repository

## Troubleshooting

### Build fails with "API key is invalid"

- Verify the `NUGET_API_KEY` secret is set correctly in GitHub
- Check that the API key hasn't expired on NuGet.org
- Ensure the key has "Push" permissions

### Version already exists

- NuGet doesn't allow overwriting versions
- Increment the version number and create a new tag
- Use `--skip-duplicate` flag (already included in workflow)

### Package not appearing on NuGet

- Wait 10-15 minutes for indexing
- Check NuGet.org for any validation errors
- Verify the package was uploaded in the GitHub Actions logs

## Environment Variables

You can customize these in the workflow files:

- `DOTNET_VERSION`: .NET SDK version (default: `8.0.x`)
- `PROJECT_PATH`: Path to .csproj file (default: `BlazorFlow/BlazorFlow.csproj`)
- `PACKAGE_OUTPUT_DIR`: Where packages are created (default: `packages`)

## Security Notes

- ✅ NuGet API key is stored as an encrypted secret
- ✅ Key is never exposed in logs
- ✅ Key has limited scope (only push to BlazorFlow packages)
- ⚠️ Regenerate the key if it's ever exposed

## Support

For issues with GitHub Actions:
- Check the Actions tab for detailed logs
- Review this setup guide
- Open an issue in the repository
