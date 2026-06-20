# Template for a future Homebrew tap cask.
# Replace the version, sha256, and URL after publishing a signed macOS Velopack artifact.
cask "winprint" do
  version "{{version}}"
  sha256 "{{sha256}}"

  url "https://github.com/kindel/winprint/releases/download/v#{version}/{{macosArtifactName}}"
  name "WinPrint"
  desc "Advanced source code and text file printing"
  homepage "https://github.com/kindel/winprint"

  app "WinPrint.app"
  binary "wp"
end
