<#
    WinOptimizer Core — PSScriptAnalyzer + Smoke Tests
    Run with: Invoke-Pester .\tests\ -Output Detailed
#>

BeforeAll {
    $enginePath = Join-Path $PSScriptRoot ".." "engine"
    Get-ChildItem -Path $enginePath -Filter "*.ps1" | ForEach-Object { . $_.FullName }
}

Describe "PSScriptAnalyzer — Engine Scripts" {
    BeforeAll {
        $enginePath = Join-Path $PSScriptRoot ".." "engine"
        $scripts = Get-ChildItem -Path $enginePath -Filter "*.ps1"
    }

    It "Should pass PSScriptAnalyzer on <_.Name>" -ForEach @($scripts) {
        $results = Invoke-ScriptAnalyzer -Path $_.FullName -Severity @("Error", "Warning")
        $results | Should -BeNullOrEmpty
    }
}

Describe "Get-AvailableTweaks" {
    It "Should return all tweaks when called without parameters" {
        $tweaks = Get-AvailableTweaks
        $tweaks | Should -Not -BeNullOrEmpty
        $tweaks.Count | Should -BeGreaterOrEqual 10
    }

    It "Should return tweaks for every category" {
        $categories = @("privacy", "services", "network", "gaming", "appearance", "startmenu-taskbar", "explorer")
        foreach ($cat in $categories) {
            $tweaks = Get-AvailableTweaks -Category $cat
            $tweaks | Should -Not -BeNullOrEmpty -Because "category '$cat' should have at least one tweak"
        }
    }

    It "Should return only privacy tweaks when -Category privacy" {
        $tweaks = Get-AvailableTweaks -Category "privacy"
        $tweaks | Should -Not -BeNullOrEmpty
        foreach ($t in $tweaks) {
            $t.category | Should -Be "privacy"
        }
    }

    It "Should return a single tweak by ID" {
        $tweaks = Get-AvailableTweaks -Id "disable-telemetry"
        $tweaks | Should -HaveCount 1
        $tweaks[0].id | Should -Be "disable-telemetry"
    }

    It "Should return empty for nonexistent ID" {
        $tweaks = Get-AvailableTweaks -Id "nonexistent-tweak"
        $tweaks | Should -BeNullOrEmpty
    }
}

Describe "Tweak Definitions" {
    BeforeAll {
        $tweaks = Get-AvailableTweaks
    }

    It "Every tweak should have required fields" {
        foreach ($t in $tweaks) {
            $t.id | Should -Not -BeNullOrEmpty
            $t.name | Should -Not -BeNullOrEmpty
            $t.description | Should -Not -BeNullOrEmpty
            $t.category | Should -Not -BeNullOrEmpty
            $t.commands | Should -Not -BeNullOrEmpty
            $t.commands.apply | Should -Not -BeNullOrEmpty
            $t.commands.revert | Should -Not -BeNullOrEmpty
            $t.risk | Should -BeIn @("low", "medium", "high")
            $t.reversible | Should -BeOfType [bool]
            $t.compatible_with_vanguard | Should -BeOfType [bool]
            $t.requires_restart | Should -BeOfType [bool]
        }
    }

    It "All tweaks should be reversible" {
        foreach ($t in $tweaks) {
            $t.reversible | Should -BeTrue -Because "All current tweaks should be reversible"
        }
    }

    It "All tweaks should be Vanguard compatible" {
        foreach ($t in $tweaks) {
            $t.compatible_with_vanguard | Should -BeTrue -Because "Current baseline tweaks are safe"
        }
    }

    It "IDs should be kebab-case" {
        foreach ($t in $tweaks) {
            $t.id | Should -Match "^[a-z0-9\-]+$"
        }
    }
}

Describe "Profiles" {
    It "All profile JSON files should be valid" {
        $profilesDir = Join-Path $PSScriptRoot ".." "profiles"
        $files = Get-ChildItem -Path $profilesDir -Filter "*.json"
        $files | Should -Not -BeNullOrEmpty

        foreach ($f in $files) {
            $profile = Get-Content -Path $f.FullName -Raw | ConvertFrom-Json
            $profile.name | Should -Not -BeNullOrEmpty
            $profile.tweak_ids | Should -Not -BeNullOrEmpty
        }
    }

    It "Profile tweak_ids should reference existing tweaks" {
        $profilesDir = Join-Path $PSScriptRoot ".." "profiles"
        $files = Get-ChildItem -Path $profilesDir -Filter "*.json"
        $allTweaks = Get-AvailableTweaks
        $allIds = $allTweaks | ForEach-Object { $_.id }

        foreach ($f in $files) {
            $profile = Get-Content -Path $f.FullName -Raw | ConvertFrom-Json
            foreach ($tid in $profile.tweak_ids) {
                $allIds | Should -Contain $tid -Because "Profile '$($profile.name)' references tweak '$tid'"
            }
        }
    }
}

Describe "Schemas" {
    It "All schema files should be valid JSON" {
        $schemasDir = Join-Path $PSScriptRoot ".." "shared" "schemas"
        $files = Get-ChildItem -Path $schemasDir -Filter "*.json"
        $files | Should -Not -BeNullOrEmpty

        foreach ($f in $files) {
            { Get-Content -Path $f.FullName -Raw | ConvertFrom-Json } | Should -Not -Throw
        }
    }
}

Describe "Test-VanguardInstalled" {
    It "Should return a boolean" {
        $result = Test-VanguardInstalled
        $result | Should -BeOfType [bool]
    }
}
