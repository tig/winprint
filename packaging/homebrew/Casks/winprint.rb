# Template rendered by the release pipeline (release.yml -> brew job) and pushed to the
# kindel/homebrew-winprint tap. Placeholders are filled with each stable release's version,
# download base URL, and per-arch SHA256s. This is the free MAUI GUI (WinPrint.app), a
# notarized Developer ID build distributed directly (NOT the App Store).
#
# The GUI bundle ALSO embeds the `wp` TUI (release.yml copies the self-contained CLI payload into
# WinPrint.app/Contents/Helpers/wp), so this single cask install delivers BOTH the GUI and the `wp`
# command — the `binary` stanza below symlinks the embedded wp onto PATH. The standalone Homebrew
# *formula* still ships `wp` for Linux and CLI-only installs. Both provide `wp`, so installing the
# cask and the formula together collides on the `wp` symlink (Homebrew errors at link time) — pick
# one on macOS. (Casks can't declare a `conflicts_with formula:`; that key is cask-only, so we just
# document it here instead of encoding an invalid stanza.)
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

  app "WinPrint.app"
  binary "#{appdir}/WinPrint.app/Contents/Helpers/wp/wp"

  # WinPrint.app is not yet Apple-notarized (tracked in tig/winprint#162), so Gatekeeper may
  # report it as "damaged". Remove this caveat once notarization ships.
  caveats <<~EOS
    WinPrint.app is not yet notarized by Apple. If macOS says it "is damaged and can't be
    opened", clear the download quarantine and reopen it:
      xattr -dr com.apple.quarantine "#{appdir}/WinPrint.app"
  EOS

  zap trash: [
    "~/Library/Application Support/WinPrint",
    "~/Library/Preferences/com.kindel.winprint.plist",
    "~/Library/Logs/WinPrint",
  ]
end
