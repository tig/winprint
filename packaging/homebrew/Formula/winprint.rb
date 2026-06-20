# Template rendered by the release pipeline (release.yml -> brew job) and pushed to the
# kindel/homebrew-winprint tap; the placeholders are filled with each stable release's
# version, download base URL, and per-arch SHA256s. Free TUI (wp) only — GUI ships via stores.
class Winprint < Formula
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
