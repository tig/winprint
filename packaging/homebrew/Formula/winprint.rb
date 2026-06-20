# Template for a future Homebrew tap formula.
# Replace the version, sha256, and URL after publishing a Linux Velopack artifact.
class Winprint < Formula
  desc "Advanced source code and text file printing terminal UI"
  homepage "https://github.com/kindel/winprint"
  url "https://github.com/kindel/winprint/releases/download/v{{version}}/{{linuxArtifactName}}"
  sha256 "{{sha256}}"
  license "MIT"

  def install
    libexec.install Dir["*"]
    bin.install_symlink libexec/"wp"
  end

  test do
    system "#{bin}/wp", "--version"
  end
end
