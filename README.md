<h1 align="center">Kindergarten</h1>

<p align="center">
  <img style="width: 192px" src="WpfApp/resources/icon256.png"/>
</p>


## What is this?
__Kindergarten__ is information system, which provides children management for any nursery school.

Application is divided into three project

1. Camera is simply help-app for taking photos
2. DAL is data layer, which consists of entity models. There's also migrations there
3. WpfApp is main program.

WpfApp
======

## Settings save
Window resolution and state (besides minimize) always save in __settings.json__.

You can save some settings using binding to [] with default value.

_For example_
```xml
<TextBox Text="{Binding '[font_size_tables, 12]'}" />
```
where __"12"__ is default value

Settings load when program has started and save when it has shut down _(see [App.xaml.cs](WpfApp/App.xaml.cs))_.


## There are a few steps to add a new window:
- to add new ___*.xaml___ into the _View_ directory
- to add class into the _ViewModel_ (_namespace_) directory
- to derive created view model from [ViewModelBase](WpfApp/Framework/Core/ViewModelBase.cs) or [_PipeViewModel_](WpfApp/Framework/Core/PipeViewModel.cs) classes
- in the [App.xaml](WpfApp/App.xaml) to add ___&lt;ViewViewModelPair&gt;___ into ___&lt;ViewViewModelPairs&gt;___ as child


---
## Example
#### How to open _"OtherWindow"_ from _"SomeWindow"_ uses __Command__

---
- SomeViewModel.cs
```cs
public class SomeViewModel : ViewModelBase
{
    public IRelayCommand OpenOtherCommand { get; }

    public SomeViewModel()
    {
        AddChildCommand = new RelayCommand<string>(OpenOther);
    }

    public void OpenOther(string data)
    {
        // parameters
        var pipe = new Pipe(isDialog: true);
        pipe.SetParameter("the_first_argument", 1);
        pipe.SetParameter("the_second_argument", data);

        StartViewModel<OtherViewModel>(pipe);
    }
}
```

- OtherViewModel.cs
```cs
public class OtherViewModel : ViewModelBase
{
    public override void OnLoaded()
    {
        int arg1 = (int) Pipe.GetParameter("the_first_argument");
        string arg2 = (string) Pipe.GetParameter("the_second_argument");
        string arg3 = Pipe.GetParameter("no argument", "this will be a default value");  // no cast
        MessageBox.Show($"Parameters: arg1 = {arg1}, arg2 = {arg2}, arg3 = {arg3}", "Title");
    }
    
    public override bool OnFinishing()
    {
        if (MessageBox.Show("Are you sure?", "Exit", MessageBoxButton.YesNo) != MessageBoxResult.Yes)
            return false;  // false - cancel exit
    }
}
```

- SomeWindow.xaml
```xml
<Window x:Class="WpfApp.View.SomeWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
        xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
        mc:Ignorable="d"
        Title="SomeWindow" Height="300" Width="300">
    <StackPanel>
        <TextBox Name="TextBox" Text="This is parameter for other window"/>
        <Button Content="Open other window" Command="{Binding OpenOtherCommand}" CommandParameter="{Binding ElementName=TextBox, Path=Text}"/>
    </StackPanel>
</Window>
```

DAL
===
It has models and provider to database.

There's code first approach are used _(Entity Framework)_.

Camera
======
<!-- ![Camera logo](Camera/Camera-WF.ico) -->
This project is stand-alone app, which is not depends on the others