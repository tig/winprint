# Template rendered by the release pipeline (release.yml -> brew job) and pushed to the
# kindel/homebrew-winprint tap. Placeholders are filled with each stable release's version,
# download base URL, and per-arch SHA256s. This is the free MAUI GUI (WinPrint.app), a
# notarized Developer ID build distributed directly (NOT the App Store). The TUI (`wp`) ships
# separately as the Homebrew *formula*; `brew upgrade` handles updates for both.
cask "winprint" do
  version "{{version}}"

  on_arm do
    url "{{base}}/WinPrint-osx-arm64.app.zip"
    sha256 "{{sha_cask_arm}}"
  end
  on_intel do
    url "{{base}}/WinPrint-osx-x64.app.zip"
    sha256 "{{sha_cask_x64}}"
  end

  name "WinPrint"
  desc "Advanced source code and text file printing GUI"
  homepage "https://github.com/kindel/winprint"

  app "WinPrint.app"

  zap trash: [
    "~/Library/Application Support/WinPrint",
    "~/Library/Preferences/com.kindel.winprint.plist",
    "~/Library/Logs/WinPrint",
  ]
end
