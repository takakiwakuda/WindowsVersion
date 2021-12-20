#Requires -Module WindowsVersion

using namespace System

Describe "Get-WindowsVersion" {
    It "Should get Windows version information on the local machine" {
        $version = Get-WindowsVersion

        $version | Should -BeOfType ([WindowsVersion.WindowsVersionInfo])
        $version.Edition | Should -Not -BeNullOrEmpty
        $version.Version | Should -Not -BeNullOrEmpty
        $version.InstallDate | Should -Not -BeNullOrEmpty
        $version.OSBuild | Should -Not -BeNullOrEmpty
        $version.Experience | Should -Not -BeNullOrEmpty
    }
}
