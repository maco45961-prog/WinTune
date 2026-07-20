using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using WinSetupWizard.Hardware;
using WinSetupWizard.Integration;

namespace WinSetupWizard;

public partial class MainWindow : Window
{
    private WizardState _wizard = new();
    private bool _isSpanish = true;

    public MainWindow()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        var appDir = AppDomain.CurrentDomain.BaseDirectory;
        var corePath = Path.GetFullPath(Path.Combine(appDir, "..", "..", "..", "..", "winoptimizer-core"));
        CoreClient.Initialize(corePath);

        StatusBar.Text = "Detectando hardware...";
        BackBtn.Visibility = Visibility.Collapsed;
        NextBtn.IsEnabled = false;

        var hw = await Task.Run(HardwareDetector.Detect);
        _wizard.Hardware = hw;
        _wizard.SelectedProfile = hw.RecommendedProfile;
        ShowWelcomeStep();
        UpdateButtons();

        var profiles = await Task.Run(CoreClient.GetProfiles);
        StatusBar.Text = $"Hardware: {hw.CpuModel} / {hw.GpuModel} / {hw.TotalRamGb} GB / {hw.DiskType}";
    }

    private void ShowStep(string step)
    {
        ContentArea.Children.Clear();
        var stack = new StackPanel();

        switch (step)
        {
            case "welcome": BuildWelcomeStep(stack); break;
            case "profile": BuildProfileStep(stack); break;
            case "drivers": BuildDriverStep(stack); break;
            case "apps": BuildAppStep(stack); break;
            case "apply": BuildApplyStep(stack); break;
            case "summary": BuildSummaryStep(stack); break;
        }

        ContentArea.Children.Add(stack);
    }

    private void ShowWelcomeStep() { _wizard.CurrentStep = 0; ShowStep("welcome"); }
    private void ShowProfileSelect() { _wizard.CurrentStep = 1; ShowStep("profile"); }
    private void ShowDriverStep() { _wizard.CurrentStep = 2; ShowStep("drivers"); }
    private void ShowAppInstall() { _wizard.CurrentStep = 3; ShowStep("apps"); }
    private void ShowApplyProfile() { _wizard.CurrentStep = 4; ShowStep("apply"); }
    private void ShowSummary() { _wizard.CurrentStep = 5; ShowStep("summary"); }

    private void BuildWelcomeStep(StackPanel s)
    {
        var h = _wizard.Hardware;
        s.Children.Add(new TextBlock { Text = FindRes("WelcomeTitle"), FontSize = 24, FontWeight = FontWeights.Bold, Margin = new(0, 0, 0, 12) });
        s.Children.Add(new TextBlock { Text = FindRes("WelcomeDesc"), Foreground = FindBrush("TextSecondaryBrush"), TextWrapping = TextWrapping.Wrap, Margin = new(0, 0, 0, 20) });

        var box = new Border { Background = FindBrush("BgCardBrush"), CornerRadius = new(8), Padding = new(20), Margin = new(0, 0, 0, 16) };
        var inner = new StackPanel();
        inner.Children.Add(new TextBlock { Text = FindRes("HardwareDetected") + ":", FontSize = 16, FontWeight = FontWeights.SemiBold, Margin = new(0, 0, 0, 10) });
        if (h != null)
        {
            AddLine(inner, FindRes("Cpu") + ":", h.CpuModel);
            AddLine(inner, "Núcleos:", h.CpuCoreCount.ToString());
            AddLine(inner, FindRes("Gpu") + ":", $"{h.GpuModel} ({h.GpuVendor})");
            AddLine(inner, FindRes("Ram") + ":", $"{h.TotalRamGb} GB");
            AddLine(inner, FindRes("Disk") + ":", h.DiskType);
            AddLine(inner, FindRes("Chassis") + ":", h.IsLaptop ? FindRes("Laptop") : FindRes("Desktop"));
            AddLine(inner, "Motherboard:", $"{h.MotherboardManufacturer} {h.MotherboardModel}");

            var recBox = new Border { Background = FindBrush("AccentBrush"), CornerRadius = new(8), Padding = new(16, 10), Margin = new(0, 12, 0, 0) };
            recBox.Child = new TextBlock { Text = $"{FindRes("RecommendedProfile")}: {h.RecommendedProfileDisplay}", Foreground = Brushes.White, FontWeight = FontWeights.Bold, FontSize = 15 };
            inner.Children.Add(recBox);
        }
        box.Child = inner;
        s.Children.Add(box);
        StatusBar.Text = FindRes("Step") + " 1/6: " + FindRes("WelcomeTitle");
    }

    private void BuildProfileStep(StackPanel s)
    {
        s.Children.Add(new TextBlock { Text = FindRes("SelectProfile"), FontSize = 22, FontWeight = FontWeights.Bold });
        s.Children.Add(new TextBlock { Text = FindRes("ProfileDesc"), Foreground = FindBrush("TextSecondaryBrush"), Margin = new(0, 4, 0, 16) });

        var profiles = CoreClient.GetProfiles();
        foreach (var p in profiles)
        {
            var wrap = new Border { Background = FindBrush("BgCardBrush"), CornerRadius = new(8), Padding = new(16), Margin = new(0, 0, 0, 8) };
            var radio = new RadioButton
            {
                Content = p.Name,
                FontWeight = FontWeights.Bold,
                FontSize = 16,
                Tag = p.Name,
                IsChecked = p.Name == _wizard.SelectedProfile
            };
            radio.Checked += (o, _) => { _wizard.SelectedProfile = ((RadioButton)o!).Tag?.ToString() ?? ""; };

            var inner = new StackPanel();
            inner.Children.Add(radio);
            inner.Children.Add(new TextBlock { Text = p.Description, Foreground = FindBrush("TextSecondaryBrush"), Margin = new(24, 4, 0, 0), TextWrapping = TextWrapping.Wrap });
            inner.Children.Add(new TextBlock { Text = $"Riesgo: {p.RiskLevel} | Reinicio: {(p.EstimatedRestartRequired ? "Sí" : "No")}", Foreground = FindBrush("TextMutedBrush"), Margin = new(24, 4, 0, 0), FontSize = 12 });
            wrap.Child = inner;
            s.Children.Add(wrap);
        }

        if (_wizard.Hardware?.TotalRamGb <= 4)
        {
            var note = new Border { Background = FindBrush("WarningBrush"), CornerRadius = new(8), Padding = new(12), Margin = new(0, 8, 0, 0) };
            note.Child = new TextBlock { Foreground = Brushes.White, TextWrapping = TextWrapping.Wrap, Text = "⚠ RAM baja (≤4 GB). Recomendamos Gaming Low-End." };
            s.Children.Add(note);
        }
        StatusBar.Text = FindRes("Step") + " 2/6: " + FindRes("SelectProfile");
    }

    private void BuildDriverStep(StackPanel s)
    {
        s.Children.Add(new TextBlock { Text = FindRes("DriverInstall"), FontSize = 22, FontWeight = FontWeights.Bold });
        s.Children.Add(new TextBlock { Text = FindRes("DriverDesc"), Foreground = FindBrush("TextSecondaryBrush"), Margin = new(0, 4, 0, 16), TextWrapping = TextWrapping.Wrap });

        var h = _wizard.Hardware;
        if (h != null)
        {
            AddDriverBtn(s, FindRes("Gpu") + ": " + h.GpuModel, h.GpuDriverUrl);
            AddDriverBtn(s, "CPU (" + h.CpuManufacturer + ")", h.CpuDriverUrl);
            if (!string.IsNullOrEmpty(h.ChipsetDriverUrl))
                AddDriverBtn(s, "Chipset (" + h.MotherboardManufacturer + ")", h.ChipsetDriverUrl);
        }

        s.Children.Add(new TextBlock { Text = "Solo enlaces oficiales. Nada se descarga sin tu permiso.", Foreground = FindBrush("TextMutedBrush"), FontStyle = FontStyles.Italic, Margin = new(0, 12, 0, 0) });
        StatusBar.Text = FindRes("Step") + " 3/6: " + FindRes("DriverInstall");
    }

    private void BuildAppStep(StackPanel s)
    {
        s.Children.Add(new TextBlock { Text = FindRes("AppInstall"), FontSize = 22, FontWeight = FontWeights.Bold });
        s.Children.Add(new TextBlock { Text = FindRes("AppDesc"), Foreground = FindBrush("TextSecondaryBrush"), Margin = new(0, 4, 0, 16) });

        var apps = CoreClient.LoadCuratedApps();
        var scroll = new ScrollViewer { MaxHeight = 350 };
        var inner = new StackPanel();

        foreach (var app in apps)
        {
            var chk = new CheckBox
            {
                Content = $"{app.Name}  ({app.Category})",
                Tag = app.Id,
                IsChecked = app.Recommended,
                FontSize = 14,
                Margin = new(0, 2, 0, 0)
            };
            chk.Checked += (_, _) => { if (!_wizard.SelectedApps.Contains(app.Id)) _wizard.SelectedApps.Add(app.Id); };
            chk.Unchecked += (_, _) => _wizard.SelectedApps.Remove(app.Id);
            if (app.Recommended && !_wizard.SelectedApps.Contains(app.Id))
                _wizard.SelectedApps.Add(app.Id);

            var item = new Border { Background = FindBrush("BgTertiaryBrush"), CornerRadius = new(6), Padding = new(8, 4), Margin = new(0, 0, 0, 2) };
            var itemStack = new StackPanel();
            itemStack.Children.Add(chk);
            itemStack.Children.Add(new TextBlock { Text = app.Description, Foreground = FindBrush("TextMutedBrush"), FontSize = 11, Margin = new(24, 0, 0, 2) });
            item.Child = itemStack;
            inner.Children.Add(item);
        }

        scroll.Content = inner;
        s.Children.Add(scroll);
        StatusBar.Text = FindRes("Step") + " 4/6: " + FindRes("AppInstall");
    }

    private void BuildApplyStep(StackPanel s)
    {
        s.Children.Add(new TextBlock { Text = FindRes("ApplyProfileStep"), FontSize = 22, FontWeight = FontWeights.Bold });
        s.Children.Add(new TextBlock { Text = FindRes("ApplyProfileDesc"), Foreground = FindBrush("TextSecondaryBrush"), Margin = new(0, 4, 0, 16), TextWrapping = TextWrapping.Wrap });

        var profile = CoreClient.GetProfiles().FirstOrDefault(p => p.Name == _wizard.SelectedProfile);
        var pName = profile?.Name ?? _wizard.SelectedProfile;
        var tweakCount = profile?.TweakIds.Count ?? 0;
        var restart = profile?.EstimatedRestartRequired == true;

        var box = new Border { Background = FindBrush("BgCardBrush"), CornerRadius = new(8), Padding = new(16), Margin = new(0, 0, 0, 12) };
        var inner = new StackPanel();
        inner.Children.Add(new TextBlock { Text = "Perfil: " + pName, FontWeight = FontWeights.Bold, FontSize = 16 });
        if (profile != null)
            inner.Children.Add(new TextBlock { Text = profile.Description, Foreground = FindBrush("TextSecondaryBrush"), TextWrapping = TextWrapping.Wrap });
        inner.Children.Add(new TextBlock { Text = "Tweaks a aplicar: " + tweakCount, Margin = new(0, 8, 0, 0) });
        inner.Children.Add(new TextBlock { Text = "Requiere reinicio: " + (restart ? "Sí" : "No") });

        if (restart)
        {
            var rw = new Border { Background = FindBrush("WarningBrush"), CornerRadius = new(6), Padding = new(8, 4), Margin = new(0, 8, 0, 0) };
            rw.Child = new TextBlock { Text = FindRes("RestartNeeded"), Foreground = Brushes.White, FontWeight = FontWeights.Bold };
            inner.Children.Add(rw);
        }
        box.Child = inner;
        s.Children.Add(box);

        var applyBtn = new Button { Content = FindRes("ApplyNow"), Style = FindResource("PrimaryButton") as Style, Margin = new(0, 0, 0, 8) };
        applyBtn.Click += async (_, _) =>
        {
            applyBtn.IsEnabled = false;
            NextBtn.IsEnabled = false;
            StatusBar.Text = "Aplicando perfil...";
            var resultStr = await Task.Run(() => WizardRunner.ApplyProfileViaPowerShell(_wizard.SelectedProfile, CoreClient.CorePath ?? ""));
            var parts = resultStr.Split('|');
            if (parts[0] == "success" || parts[0] == "partial")
            {
                _wizard.ProfileResult = new ApplyProfileResult
                {
                    Status = parts[0],
                    Applied = int.Parse(parts[1]),
                    Errors = int.Parse(parts[2]),
                    Total = int.Parse(parts[3]),
                    Profile = _wizard.SelectedProfile,
                    Message = parts.Length > 4 ? parts[4] : ""
                };
                StatusBar.Text = $"✓ Perfil aplicado: {_wizard.ProfileResult.Applied}/{_wizard.ProfileResult.Total} tweaks";
                applyBtn.IsEnabled = false;
            }
            else
            {
                StatusBar.Text = "✗ " + resultStr;
                applyBtn.IsEnabled = true;
            }
            NextBtn.IsEnabled = true;
        };
        s.Children.Add(applyBtn);

        StatusBar.Text = FindRes("Step") + " 5/6: " + FindRes("ApplyProfileStep");
    }

    private void BuildSummaryStep(StackPanel s)
    {
        s.Children.Add(new TextBlock { Text = FindRes("Summary"), FontSize = 22, FontWeight = FontWeights.Bold });
        s.Children.Add(new TextBlock { Text = FindRes("SummaryDesc"), Foreground = FindBrush("TextSecondaryBrush"), Margin = new(0, 4, 0, 16) });

        // What was done
        var doneBox = new Border { Background = FindBrush("BgCardBrush"), CornerRadius = new(8), Padding = new(16), Margin = new(0, 0, 0, 12) };
        var doneStack = new StackPanel();
        doneStack.Children.Add(new TextBlock { Text = FindRes("WhatWasDone") + ":", FontSize = 16, FontWeight = FontWeights.SemiBold, Margin = new(0, 0, 0, 8) });

        doneStack.Children.Add(new TextBlock { Text = "✓ Hardware detectado", Foreground = FindBrush("SuccessBrush") });
        doneStack.Children.Add(new TextBlock { Text = "✓ Perfil seleccionado: " + _wizard.SelectedProfile, Foreground = FindBrush("SuccessBrush") });

        if (_wizard.ProfileResult != null)
        {
            var r = _wizard.ProfileResult;
            doneStack.Children.Add(new TextBlock { Text = $"✓ Perfil aplicado: {r.Applied}/{r.Total} tweaks", Foreground = FindBrush("SuccessBrush") });
            if (r.Errors > 0)
                doneStack.Children.Add(new TextBlock { Text = $"⚠ {r.Errors} errores", Foreground = FindBrush("WarningBrush") });
        }

        if (_wizard.SelectedApps.Count > 0)
            doneStack.Children.Add(new TextBlock { Text = $"✓ {_wizard.SelectedApps.Count} apps seleccionadas para instalar via winget", Foreground = FindBrush("SuccessBrush") });

        doneBox.Content = doneStack;
        s.Children.Add(doneBox);

        // What to do next
        var nextBox = new Border { Background = FindBrush("BgCardBrush"), CornerRadius = new(8), Padding = new(16) };
        var nextStack = new StackPanel();
        nextStack.Children.Add(new TextBlock { Text = FindRes("WhatToDoNext") + ":", FontSize = 16, FontWeight = FontWeights.SemiBold, Margin = new(0, 0, 0, 8) });

        var items = new List<string>();
        if (_wizard.Hardware?.GpuDriverUrl != null)
            items.Add($"{FindRes("Download")} driver GPU: {_wizard.Hardware.GpuDriverUrl}");

        var prof = CoreClient.GetProfiles().FirstOrDefault(p => p.Name == _wizard.SelectedProfile);
        if (prof?.EstimatedRestartRequired == true)
            items.Add(FindRes("RestartNeeded"));

        items.Add(FindRes("OpenOptimizer") + " para ajustes finos");

        foreach (var item in items)
        {
            nextStack.Children.Add(new TextBlock { Text = "☐ " + item, Foreground = FindBrush("TextSecondaryBrush"), TextWrapping = TextWrapping.Wrap, Margin = new(0, 0, 0, 4) });
        }

        nextBox.Content = nextStack;
        s.Children.Add(nextBox);

        StatusBar.Text = "✓ " + FindRes("Summary") + " — ¡Sistema listo!";
    }

    private void AddLine(StackPanel parent, string label, string value)
    {
        var row = new Grid();
    row.ColumnDefinitions.Add(new ColumnDefinition { Width = new(120) });
    row.ColumnDefinitions.Add(new ColumnDefinition());
        var l = new TextBlock { Text = label, Foreground = FindBrush("TextMutedBrush") };
        Grid.SetColumn(l, 0);
        var v = new TextBlock { Text = value, Foreground = FindBrush("TextPrimaryBrush") };
        Grid.SetColumn(v, 1);
        row.Children.Add(l);
        row.Children.Add(v);
        row.Margin = new(0, 2, 0, 2);
        parent.Children.Add(row);
    }

    private void AddDriverBtn(StackPanel parent, string label, string url)
    {
        if (string.IsNullOrEmpty(url)) return;

        var wrap = new Border { Background = FindBrush("BgCardBrush"), CornerRadius = new(8), Padding = new(16), Margin = new(0, 0, 0, 8) };
        var row = new Grid();
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new(1, GridUnitType.Star) });
        row.ColumnDefinitions.Add(ColumnDefinition.Create(1, GridUnitType.Auto));

        var labelText = new TextBlock { Text = label, VerticalAlignment = VerticalAlignment.Center };
        var btn = new Button { Content = FindRes("Download"), Style = FindResource("PrimaryButton") as Style, Margin = new(12, 0, 0, 0), Tag = url };
        btn.Click += (_, _) => OpenUrl(btn.Tag?.ToString() ?? "");

        Grid.SetColumn(labelText, 0);
        Grid.SetColumn(btn, 1);
        row.Children.Add(labelText);
        row.Children.Add(btn);
        wrap.Child = row;
        parent.Children.Add(wrap);
    }

    private void OpenUrl(string url)
    {
        try { Process.Start(new ProcessStartInfo(url) { UseShellExecute = true }); } catch { }
    }

    private string FindRes(string key)
    {
        return TryFindResource(key)?.ToString() ?? key;
    }

    private System.Windows.Media.Brush FindBrush(string key)
    {
        return (TryFindResource(key) as System.Windows.Media.Brush) ?? System.Windows.Media.Brushes.Gray;
    }

    private void UpdateButtons()
    {
        StepProgress.Value = _wizard.CurrentStep;
        StepIndicator.Text = $"{FindRes("Step")} {_wizard.CurrentStep + 1}/6";
        NextBtn.IsEnabled = _wizard.CurrentStep < 5;
        BackBtn.Visibility = _wizard.CurrentStep > 0 && _wizard.CurrentStep < 5 ? Visibility.Visible : Visibility.Collapsed;
        NextBtn.Content = _wizard.CurrentStep == 5 ? FindRes("Finish") : FindRes("Next");
    }

    private void NextStep(object sender, RoutedEventArgs e)
    {
        if (_wizard.CurrentStep == 5) { Application.Current.Shutdown(); return; }
        _wizard.CurrentStep++;
        ShowStep(StepNameFor(_wizard.CurrentStep));
        UpdateButtons();
    }

    private void PrevStep(object sender, RoutedEventArgs e)
    {
        if (_wizard.CurrentStep > 0) _wizard.CurrentStep--;
        ShowStep(StepNameFor(_wizard.CurrentStep));
        UpdateButtons();
    }

    private string StepNameFor(int step) => step switch
    {
        0 => "welcome",
        1 => "profile",
        2 => "drivers",
        3 => "apps",
        4 => "apply",
        5 => "summary",
        _ => "welcome"
    };

    private void CancelWizard(object sender, RoutedEventArgs e) => Application.Current.Shutdown();
}
