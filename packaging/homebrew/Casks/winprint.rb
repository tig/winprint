# Template rendered by the release pipeline (release.yml -> brew job) and pushed to the
# kindel/homebrew-winprint tap. Placeholders are filled with each stable release's version,
# download base URL, and per-arch SHA256s. This is the free MAUI GUI (WinPrint.app), a
# notarized Developer ID build distributed directly (NOT the App Store).
#
# The GUI bundle ALSO embeds the `wp` TUI (release.yml copies the self-contained CLI payload into
# WinPrint.app/Contents/Helpers/wp), so this single cask install delivers BOTH the GUI and the `wp`
# command — the `binary` stanza below symlinks the embedded wp onto PATH. The standalone Homebrew
# *formula* still ships `wp` for Linux and CLI-only installs; the cask therefore conflicts with the
# formula (both would provide `wp`).
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
  desc "Advanced source code and text file printing GUI (bundles the wp TUI)"
  homepage "https://github.com/kindel/winprint"

  conflicts_with formula: "winprint"

  app "WinPrint.app"
  binary "#{appdir}/WinPrint.app/Contents/Helpers/wp/wp"

  zap trash: [
    "~/Library/Application Support/WinPrint",
    "~/Library/Preferences/com.kindel.winprint.plist",
    "~/Library/Logs/WinPrint",
  ]
end
