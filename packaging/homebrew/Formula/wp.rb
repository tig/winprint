# Template rendered by the release pipeline (release.yml -> brew job) and pushed to the
# kindel/homebrew-winprint tap; the placeholders are filled with each stable release's
# version, download base URL, and per-arch SHA256s.
#
# This formula is the standalone free TUI (`wp`) — used on Linux and for CLI-only macOS installs.
# The full macOS app ships as the tap's *cask* `winprint` (packaging/homebrew/Casks/winprint.rb),
# which embeds `wp` already, so `brew install winprint` on a Mac delivers GUI + CLI from one
# command. The formula is named `wp` (not `winprint`) on purpose: if a formula and a cask shared
# the name `winprint`, `brew install winprint` would silently pick the formula and skip the GUI.
# Both provide the `wp` symlink, so installing this formula AND the cask collides — pick one on macOS.
class Wp < Formula
  desc "Advanced source code and text file printing terminal UI"
  homepage "https://github.com/kindel/winprint"
  version "{{version}}"
  license "MIT"

  on_macos do
    on_arm do
      url "{{base}}/wp-osx-arm64.tar.gz"
      sha256 "{{sha_osx_arm}}"
    end
    on_intel do
      url "{{base}}/wp-osx-x64.tar.gz"
      sha256 "{{sha_osx_x64}}"
    end
  end

  on_linux do
    on_arm do
      url "{{base}}/wp-linux-arm64.tar.gz"
      sha256 "{{sha_linux_arm}}"
    end
    on_intel do
      url "{{base}}/wp-linux-x64.tar.gz"
      sha256 "{{sha_linux_x64}}"
    end
  end

  def install
    libexec.install Dir["*"]
    bin.install_symlink libexec/"wp"
  end

  test do
    system bin/"wp", "--version"
  end
end
