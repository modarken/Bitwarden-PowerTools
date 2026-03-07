# Contributing to Bitwarden PowerTools

Thanks for your interest in contributing! 🎉

## Ways to Contribute

- 🐛 **Report bugs** - Use the [bug report template](https://github.com/modarken/Bitwarden-PowerTools/issues/new?template=bug_report.md)
- 💡 **Suggest features** - Use the [feature request template](https://github.com/modarken/Bitwarden-PowerTools/issues/new?template=feature_request.md)
- 📝 **Improve documentation** - Fix typos, clarify instructions, add examples
- 💻 **Submit code** - Bug fixes, new features, performance improvements

## Getting Started

### Building from Source

```powershell
git clone https://github.com/modarken/Bitwarden-PowerTools.git
cd Bitwarden-PowerTools
dotnet restore
dotnet build
```

See [@docs/Building-and-Releasing.md](@docs/Building-and-Releasing.md) for detailed build instructions.

## Submitting Changes

### For Small Changes (typos, docs, minor fixes)
1. Fork the repository
2. Make your changes
3. Submit a pull request

### For Larger Changes (new features, major refactoring)
1. **Open an issue first** to discuss the approach
2. Wait for feedback before investing significant time
3. Fork and implement once we've agreed on direction
4. Submit a pull request referencing the issue

## Code Guidelines

- Follow existing code style and patterns
- Add XML comments for public APIs
- Include error handling for edge cases
- Test your changes manually before submitting
- Keep commits focused and well-described

## Pull Request Process

Use a conventional branch and PR flow:

1. Branch from `main`
2. Use a focused branch name such as `feature/...`, `fix/...`, or `docs/...`
3. Keep the branch scoped to one change set
4. Run the relevant validation before opening the PR
5. Open a PR into `main`
6. Merge the PR before doing any release tagging

Before opening a PR:

1. Update `README.md` if functionality changed
2. Update maintainer docs in `@docs/` if workflow or release behavior changed
3. Make sure the project builds without errors
4. Include tests when the change affects behavior
5. Describe what the PR does, why it exists, and how it was validated

Release work should happen after PR merge on `main`, not on the feature branch. See `@docs/Building-and-Releasing.md`.

## Questions?

Not sure about something? Open a [discussion](https://github.com/modarken/Bitwarden-PowerTools/discussions) and ask!

## Code of Conduct

Be respectful, constructive, and professional. We're all here to build something useful together.

---

**Note**: This project is maintained in spare time. Response times may vary, but all contributions are appreciated! 🙏
