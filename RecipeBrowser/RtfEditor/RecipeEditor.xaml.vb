' Die Vorlage "Leere Seite" ist unter http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409 dokumentiert.

Imports System.Reflection
Imports Microsoft.Graphics.Canvas.Text
Imports Windows.Storage.Streams
Imports Windows.UI
Imports Windows.UI.Text
Imports Windows.UI.Xaml.Documents
Imports RtfEditor
Imports Windows.ApplicationModel.DataTransfer
Imports Windows.UI.Core
Imports Windows.Foundation.Metadata
Imports Windows.Storage
Imports Windows.Storage.Provider
Imports Windows.UI.Popups
Imports RecipeBrowser.Controls

''' <summary>
''' Eine leere Seite, die eigenständig verwendet oder zu der innerhalb eines Rahmens navigiert werden kann.
''' </summary>
Public NotInheritable Class RecipeEditor
    Inherits Page
    Implements INotifyPropertyChanged

#Region "Properties"
    Public Event PropertyChanged(ByVal sender As Object, ByVal e As PropertyChangedEventArgs) Implements INotifyPropertyChanged.PropertyChanged

    Protected Sub OnPropertyChanged(ByVal PropertyName As String)
        ' Raise the event, and make this procedure
        ' overridable, should someone want to inherit from
        ' this class and override this behavior:
        RaiseEvent PropertyChanged(Me, New PropertyChangedEventArgs(PropertyName))
    End Sub

    Public Property TimerController As Timers.Controller

    Public Property FontFamilyList As New List(Of FontFamilyDescriptor)
    Public Property FontWeightList As New List(Of Integer)
    Public Property DesignColorList As New List(Of FontColorDescriptor)
    Public Property StandardColorList As New List(Of FontColorDescriptor)
    Public Property EnumTypeList As New List(Of EnumTypeDescriptor)
    Public Property LineSpacingList As New List(Of Decimal)
    Public Property ParagraphTemplateList As New List(Of FontFamilyDescriptor)

    Private _GroupedFonts As New ObservableCollection(Of FontFamilyGroup)()

    Public ReadOnly Property GroupedFonts As ObservableCollection(Of FontFamilyGroup)
        Get
            Return _GroupedFonts
        End Get
    End Property

    Private Header1Format As FontFamilyDescriptor

    Private CurrentRecipe As Recipe

    Private _RecipeChanged As Boolean
    Private Property RecipeChanged As Boolean
        Get
            Return _RecipeChanged
        End Get
        Set(value As Boolean)
            If value <> _RecipeChanged Then
                _RecipeChanged = value
                OnPropertyChanged("RecipeChanged")
            End If
        End Set
    End Property
#End Region

#Region "NavigationHelper"

    ''' <summary>
    ''' NavigationHelper wird auf jeder Seite zur Unterstützung bei der Navigation verwendet und 
    ''' Verwaltung der Prozesslebensdauer
    ''' </summary>
    Public ReadOnly Property NavigationHelper As Common.NavigationHelper
        Get
            Return Me._navigationHelper
        End Get
    End Property
    Private _navigationHelper As Common.NavigationHelper

#End Region

#Region "Initialization"
    Public Sub New()
        InitializeComponent()

        Me._navigationHelper = New Common.NavigationHelper(Me)
        AddHandler Me._navigationHelper.LoadState, AddressOf NavigationHelper_LoadStateAsync
        AddHandler Me._navigationHelper.SaveState, AddressOf NavigationHelper_SaveState

        TimerController = Timers.Controller.Current

        SetupFontList()
        GroupedFontsCVS.Source = GroupedFonts
        FontListView.ItemsSource = GroupedFontsCVS.View
        SetupFontWeightList()
        SetupFontColorList()
        SetupEnumTypeList()
        SetupLineSpacingList()
        SetupParagraphTemplateList()
        Texteditor.Focus(FocusState.Programmatic)

        'Texteditor.Document.SetText(TextSetOptions.ApplyRtfDocumentDefaults, "Rezepttitel")
        'Dim defaultcharFormatting As ITextCharacterFormat = Texteditor.Document.GetDefaultCharacterFormat()
        'defaultcharFormatting.Name = "Calibri"
        'defaultcharFormatting.Size = 11
        'Texteditor.Document.SetDefaultCharacterFormat(defaultcharFormatting)
        'ChangeTextFormat(
        '    Function(f As ITextCharacterFormat)
        '        Dim charFormatting As ITextCharacterFormat = f
        '        charFormatting.Name = "Calibri"
        '        charFormatting.Size = 11
        '        Return f
        '    End Function)
    End Sub

    ''' <summary>
    ''' Füllt die Seite mit Inhalt auf, der bei der Navigation übergeben wird.  Gespeicherte Zustände werden ebenfalls
    ''' bereitgestellt, wenn eine Seite aus einer vorherigen Sitzung neu erstellt wird.
    ''' </summary>
    ''' 
    ''' Die Quelle des Ereignisses, normalerweise <see cref="NavigationHelper"/>
    ''' 
    ''' <param name="e">Ereignisdaten, die die Navigationsparameter bereitstellen, die an
    ''' <see cref="Frame.Navigate"/> übergeben wurde, als diese Seite ursprünglich angefordert wurde und
    ''' ein Wörterbuch des Zustands, der von dieser Seite während einer früheren
    ''' beibehalten wurde.  Der Zustand ist beim ersten Aufrufen einer Seite NULL.</param>
    ''' 

    Private Async Sub NavigationHelper_LoadStateAsync(sender As Object, e As Common.LoadStateEventArgs)

        Dim categories = DirectCast(App.Current.Resources("recipeFolders"), RecipeFolders)

        Dim key As String
        Dim folder As String
        Dim category As String
        Dim name As String

        key = DirectCast(e.NavigationParameter, String)
        Recipe.GetCategoryAndNameFromKey(key, folder, category, name)

        CurrentRecipe = Await categories.GetRecipeAsync(folder, category, name)

        Await LoadRecipeSource()

    End Sub

    ''' <summary>
    ''' Behält den dieser Seite zugeordneten Zustand bei, wenn die Anwendung angehalten oder
    ''' die Seite im Navigationscache verworfen wird.  Die Werte müssen den Serialisierungsanforderungen
    ''' von <see cref="Common.SuspensionManager.SessionState"/> entsprechen.
    ''' </summary>
    ''' <param name="sender">
    ''' Die Quelle des Ereignisses, normalerweise <see cref="NavigationHelper"/>
    ''' </param>
    ''' <param name="e">Ereignisdaten, die ein leeres Wörterbuch zum Auffüllen bereitstellen 
    ''' serialisierbarer Zustand.</param>
    Private Sub NavigationHelper_SaveState(sender As Object, e As Common.SaveStateEventArgs)
        ' TODO: Einen serialisierbaren Navigationsparameter ableiten und ihn
        ' e.PageState("EditorContent") = Texteditor.Document
    End Sub

#Region "NavigationHelper-Registrierung"

    ''' Die in diesem Abschnitt bereitgestellten Methoden werden einfach verwendet, um
    ''' damit NavigationHelper auf die Navigationsmethoden der Seite reagieren kann.
    ''' 
    ''' Platzieren Sie seitenspezifische Logik in Ereignishandlern für  
    ''' <see cref="Common.NavigationHelper.LoadState"/>
    ''' and <see cref="Common.NavigationHelper.SaveState"/>.
    ''' Der Navigationsparameter ist in der LoadState-Methode verfügbar 
    ''' zusätzlich zum Seitenzustand, der während einer früheren Sitzung beibehalten wurde.

    Protected Overrides Sub OnNavigatingFrom(e As NavigatingCancelEventArgs)
        e.Cancel = RecipeChanged
    End Sub

    Protected Overrides Sub OnNavigatedTo(e As NavigationEventArgs)
        Dim currentView = SystemNavigationManager.GetForCurrentView()

        currentView.AppViewBackButtonVisibility = AppViewBackButtonVisibility.Collapsed

        _navigationHelper.OnNavigatedTo(e)
    End Sub


    Protected Overrides Sub OnNavigatedFrom(e As NavigationEventArgs)
        _navigationHelper.OnNavigatedFrom(e)
    End Sub

#End Region

    Private Sub SetupEnumTypeList()

        EnumTypeList.Add(New EnumTypeDescriptor(MarkerType.None, MarkerStyle.Undefined, vbCrLf + "  Ohne"))
        EnumTypeList.Add(New EnumTypeDescriptor(MarkerType.Arabic, MarkerStyle.Period, "1. ---" + vbCrLf + "2. ---" + vbCrLf + "3. ---"))
        EnumTypeList.Add(New EnumTypeDescriptor(MarkerType.Arabic, MarkerStyle.Parenthesis, "1) ---" + vbCrLf + "2) ---" + vbCrLf + "3) ---"))
        EnumTypeList.Add(New EnumTypeDescriptor(MarkerType.UppercaseRoman, MarkerStyle.Period, "I. ---" + vbCrLf + "II. ---" + vbCrLf + "III. ---"))
        EnumTypeList.Add(New EnumTypeDescriptor(MarkerType.Arabic, MarkerStyle.Parentheses, "(1) ---" + vbCrLf + "(2) ---" + vbCrLf + "(3) ---"))
        EnumTypeList.Add(New EnumTypeDescriptor(MarkerType.LowercaseEnglishLetter, MarkerStyle.Parenthesis, "a) ---" + vbCrLf + "b) ---" + vbCrLf + "c) ---"))
        EnumTypeList.Add(New EnumTypeDescriptor(MarkerType.LowercaseEnglishLetter, MarkerStyle.Period, "a. ---" + vbCrLf + "b. ---" + vbCrLf + "c. ---"))
        EnumTypeList.Add(New EnumTypeDescriptor(MarkerType.LowercaseRoman, MarkerStyle.Period, "i. ---" + vbCrLf + "i. ---" + vbCrLf + "iii. ---"))

    End Sub

    Private Sub SetupLineSpacingList()
        LineSpacingList.Add(1D)
        LineSpacingList.Add(1.15D)
        LineSpacingList.Add(1.5D)
        LineSpacingList.Add(2D)
        LineSpacingList.Add(2.5D)
        LineSpacingList.Add(3D)
    End Sub

    Private Sub SetupFontColorList()
        Dim c As Color = Color.FromArgb(&HFF, 0, 0, 0)

        Dim designColors = ColorUtilities.DesignColors
        Dim standardColors = ColorUtilities.StandardColors

        For Each item In designColors
            DesignColorList.Add(New FontColorDescriptor(item))
        Next

        For Each item In standardColors
            StandardColorList.Add(New FontColorDescriptor(item))
        Next
    End Sub

    Private Sub SetupFontWeightList()

        FontWeightList.Add(8)
        FontWeightList.Add(9)
        FontWeightList.Add(10)
        FontWeightList.Add(11)
        FontWeightList.Add(12)
        FontWeightList.Add(14)
        FontWeightList.Add(16)
        FontWeightList.Add(18)
        FontWeightList.Add(20)
        FontWeightList.Add(22)
        FontWeightList.Add(24)
        FontWeightList.Add(26)
        FontWeightList.Add(28)
        FontWeightList.Add(36)
        FontWeightList.Add(48)
        FontWeightList.Add(72)

    End Sub

    Private Sub SetupParagraphTemplateList()

        Dim BlueText As SolidColorBrush = DirectCast(RecipeBrowser.App.Current.Resources("BlueTextBackgroundBrush"), SolidColorBrush)
        Dim BlackText As SolidColorBrush = New SolidColorBrush(Colors.Black)
        Dim GreyText As SolidColorBrush = New SolidColorBrush(Colors.DarkGray)

        Header1Format = New FontFamilyDescriptor(App.Texts.GetString("Heading1"), "Calibri Light", 18, FontWeights.Normal, BlueText)

        ParagraphTemplateList.Add(New FontFamilyDescriptor(App.Texts.GetString("Standard"), "Calibri", 12, FontWeights.Normal, BlackText))
        ParagraphTemplateList.Add(New FontFamilyDescriptor(App.Texts.GetString("Title"), "Calibri Light", 20, FontWeights.Normal, BlueText))
        ParagraphTemplateList.Add(New FontFamilyDescriptor(App.Texts.GetString("SubTitle"), "Calibri", 14, FontWeights.Bold, GreyText))
        ParagraphTemplateList.Add(Header1Format)
        ParagraphTemplateList.Add(New FontFamilyDescriptor(App.Texts.GetString("Heading1"), "Calibri Light", 16, FontWeights.Normal, BlueText))
        ParagraphTemplateList.Add(New FontFamilyDescriptor(App.Texts.GetString("TitleLarge"), "Calibri Light", 30, FontWeights.Normal, BlueText))
        ParagraphTemplateList.Add(New FontFamilyDescriptor(App.Texts.GetString("BoldTitle"), "Calibri", 12, FontWeights.Bold, BlackText))

    End Sub

#End Region

#Region "UtilitiesChangeFormat"
    Private Sub ChangeTextFormat(op As Func(Of ITextCharacterFormat, ITextCharacterFormat))

        Dim selectedText As ITextSelection = Texteditor.Document.Selection
        If selectedText IsNot Nothing Then
            Dim charFormatting As ITextCharacterFormat = selectedText.CharacterFormat
            charFormatting = op(charFormatting)
            selectedText.CharacterFormat = charFormatting
            RecipeChanged = True
        End If

    End Sub

    Private Sub ChangeParagraphFormat(op As Func(Of ITextParagraphFormat, ITextParagraphFormat))

        Dim selectedText As ITextSelection = Texteditor.Document.Selection
        If selectedText IsNot Nothing Then
            Dim parFormatting As ITextParagraphFormat = selectedText.ParagraphFormat
            parFormatting = op(parFormatting)
            selectedText.ParagraphFormat = parFormatting
            RecipeChanged = True
        End If

    End Sub

#End Region

#Region "Texteditor"
    Private Sub Texteditor_SelectionChanged(sender As Object, e As RoutedEventArgs) Handles Texteditor.SelectionChanged

        Dim selectedText As ITextSelection = Texteditor.Document.Selection
        If selectedText IsNot Nothing Then
            Dim parFormatting As ITextParagraphFormat = selectedText.ParagraphFormat
            Itemize.IsChecked = (parFormatting.ListType = MarkerType.Bullet)

            Dim charFormatting As ITextCharacterFormat = selectedText.CharacterFormat
            Bold.IsChecked = charFormatting.Bold
            Italics.IsChecked = charFormatting.Italic
            Underline.IsChecked = (charFormatting.Underline = UnderlineType.Single)

            Header1.IsChecked = (charFormatting.Size = Header1Format.FontSize AndAlso
                                  charFormatting.Name = Header1Format.FontFamilyName AndAlso
                                  charFormatting.Weight = Header1Format.FontWeight.Weight AndAlso
                                  charFormatting.ForegroundColor = Header1Format.ForegroundColorBrush.Color)
        End If

    End Sub

    Private Sub Texteditor_TextChanged(sender As Object, e As RoutedEventArgs) Handles Texteditor.TextChanged

        If Texteditor.Document.CanUndo() Then
            Undo.Visibility = Visibility.Visible
            RecipeChanged = True
        Else
            Undo.Visibility = Visibility.Collapsed
        End If

        If Texteditor.Document.CanRedo() Then
            Redo.Visibility = Visibility.Visible
        Else
            Redo.Visibility = Visibility.Collapsed
        End If

    End Sub

#End Region

#Region "CharacterFormat"
    Private Sub Bold_Click(sender As Object, e As RoutedEventArgs)
        ChangeTextFormat(
            Function(f As ITextCharacterFormat)
                Dim charFormatting As ITextCharacterFormat = f
                charFormatting.Bold = FormatEffect.Toggle
                Return f
            End Function)
        FontFormattingFlyout.Hide()
        Texteditor.Focus(FocusState.Programmatic)
    End Sub

    Private Sub Italics_Click(sender As Object, e As RoutedEventArgs)
        ChangeTextFormat(
            Function(f As ITextCharacterFormat)
                Dim charFormatting As ITextCharacterFormat = f
                charFormatting.Italic = FormatEffect.Toggle
                Return f
            End Function)
        FontFormattingFlyout.Hide()
        Texteditor.Focus(FocusState.Programmatic)
    End Sub

    Private Sub Underline_Click(sender As Object, e As RoutedEventArgs)
        ChangeTextFormat(
            Function(f As ITextCharacterFormat)
                Dim charFormatting As ITextCharacterFormat = f
                If Underline.IsChecked Then
                    charFormatting.Underline = UnderlineType.Single
                Else
                    charFormatting.Underline = UnderlineType.None
                End If
                Return f
            End Function)
        FontFormattingFlyout.Hide()
        Texteditor.Focus(FocusState.Programmatic)
    End Sub


#End Region

#Region "FontSelection"
    Public Sub SetupFontList()

        Dim FontsGroup As FontFamilyGroup
        Dim descr As FontFamilyDescriptor

        GroupedFonts.Clear()

        FontsGroup = New FontFamilyGroup
        FontsGroup.Key = App.Texts.GetString("DesignFonts")

        descr = New FontFamilyDescriptor
        descr.FontFamilyName = "Calibri Light"
        descr.Title = "Calibri Light (" + App.Texts.GetString("Headings") + ")"
        descr.FontSize = 14
        descr.FontWeight = FontWeights.Normal
        FontsGroup.Add(descr)

        descr = New FontFamilyDescriptor
        descr.FontFamilyName = "Calibri"
        descr.Title = "Calibri (" + App.Texts.GetString("TextBody") + ")"
        descr.FontSize = 14
        descr.FontWeight = FontWeights.Normal
        FontsGroup.Add(descr)

        GroupedFonts.Add(FontsGroup)

        If FavoriteFonts.Current.LastUsedFonts.Count > 0 Then
            FontsGroup = New FontFamilyGroup
            FontsGroup.Key = App.Texts.GetString("RecentlyUsedFonts")

            For Each f In FavoriteFonts.Current.LastUsedFonts
                descr = New FontFamilyDescriptor
                descr.FontFamilyName = f
                descr.Title = f
                descr.FontSize = 14
                descr.FontWeight = FontWeights.Normal
                FontsGroup.Add(descr)
            Next
            GroupedFonts.Add(FontsGroup)
        End If

        ' Enumerate the current set of system fonts,
        ' and fill the combo box with the names of the fonts.
        Dim allFonts As New List(Of String)
        For Each entry In CanvasTextFormat.GetSystemFontFamilies()
            allFonts.Add(entry)
        Next
        allFonts.Sort()
        FontsGroup = New FontFamilyGroup
        FontsGroup.Key = App.Texts.GetString("AllFonts")
        For Each FontFamilyName In allFonts
            ' FontFamily.Source contains the font family name.
            descr = New FontFamilyDescriptor
            descr.FontFamilyName = FontFamilyName
            descr.Title = FontFamilyName
            descr.FontSize = 14
            descr.FontWeight = FontWeights.Normal
            FontsGroup.Add(descr)
            FontFamilyList.Add(descr)
        Next
        GroupedFonts.Add(FontsGroup)
    End Sub

    Private Sub FontListView_ItemClick(sender As Object, e As ItemClickEventArgs) Handles FontListView.ItemClick
        Dim clickedItem As FontFamilyDescriptor = DirectCast(e.ClickedItem, FontFamilyDescriptor)
        FontInputBox.Text = clickedItem.Title

        FontSelectorFlyout.Hide()

        ChangeTextFormat(
            Function(f As ITextCharacterFormat)
                Dim charFormatting As ITextCharacterFormat = f
                charFormatting.Name = clickedItem.FontFamilyName
                Return f
            End Function)

        FavoriteFonts.Current.AddFont(clickedItem.FontFamilyName)

        SetupFontList()

        FontFormattingFlyout.ShowAt(FontFormatting)
    End Sub

#End Region

#Region "FontFormattingFlyout"
    Private Sub FontWeightComboBox_SelectionChanged(sender As Object, e As SelectionChangedEventArgs) Handles FontWeightComboBox.SelectionChanged
        ChangeTextFormat(
            Function(f As ITextCharacterFormat)
                Dim charFormatting As ITextCharacterFormat = f
                charFormatting.Size = FontWeightComboBox.SelectedValue
                Return f
            End Function)
    End Sub

    Private Sub FontFormattingFlyout_Opening(sender As Object, e As Object) Handles FontFormattingFlyout.Opening
        Dim selectedText As ITextSelection = Texteditor.Document.Selection
        If selectedText IsNot Nothing Then
            FontInputBox.Text = selectedText.CharacterFormat.Name
            Dim s As New Integer
            s = selectedText.CharacterFormat.Size
            FontWeightComboBox.SelectedValue = s
            BoldOnFlyout.IsChecked = selectedText.CharacterFormat.Bold
            ItalicsOnFlyout.IsChecked = selectedText.CharacterFormat.Italic
            UnderlineOnFlyout.IsChecked = (selectedText.CharacterFormat.Underline = UnderlineType.Single)
        End If

    End Sub

    Private Sub FontFormattingButtons_ItemClick(sender As Object, e As ItemClickEventArgs) Handles FontFormattingButtons.ItemClick
        Dim clickedItem As FrameworkElement = DirectCast(e.ClickedItem, FrameworkElement)

        Select Case clickedItem.Name
            Case "FontPropertyHighlight"
                ChangeTextFormat(
                    Function(f As ITextCharacterFormat)
                        Dim charFormatting As ITextCharacterFormat = f
                        charFormatting.BackgroundColor = Windows.UI.Colors.Yellow
                        Return f
                    End Function)
                FontFormattingFlyout.Hide()
                Texteditor.Focus(FocusState.Programmatic)

            Case "FontPropertiesReset"
                ChangeTextFormat(
                    Function(f As ITextCharacterFormat)
                        Dim charFormatting As ITextCharacterFormat = f
                        charFormatting.BackgroundColor = Windows.UI.Colors.White
                        charFormatting.ForegroundColor = Windows.UI.Colors.Black
                        charFormatting.Bold = FormatEffect.Off
                        charFormatting.Italic = FormatEffect.Off
                        charFormatting.Underline = UnderlineType.None
                        charFormatting.Name = "Segoe UI"
                        charFormatting.Size = 11
                        Return f
                    End Function)
                Underline.IsChecked = False
                Bold.IsChecked = False
                Italics.IsChecked = False
                FontFormattingFlyout.Hide()
                Texteditor.Focus(FocusState.Programmatic)

            Case "FontPropertyColor"
                FontColorChooserFlyoutBase = FlyoutBase.GetAttachedFlyout(clickedItem)
                FontColorChooserFlyoutBase.ShowAt(FontFormatting)
        End Select
    End Sub

#End Region

#Region "FontColor"
    Private FontColorChooserFlyoutBase As FlyoutBase


    Private Sub LeaveColorChooser_Click(sender As Object, e As RoutedEventArgs) Handles LeaveColorChooser.Click
        FontFormattingFlyout.ShowAt(FontFormatting)
    End Sub

    Private Sub DesignFontColorPalette_ItemClick(sender As Object, e As ItemClickEventArgs) Handles DesignFontColorPalette.ItemClick
        Dim clickedItem As FontColorDescriptor = DirectCast(e.ClickedItem, FontColorDescriptor)

        ChangeTextFormat(
                    Function(f As ITextCharacterFormat)
                        Dim charFormatting As ITextCharacterFormat = f
                        charFormatting.ForegroundColor = clickedItem.Argb
                        Return f
                    End Function)
        FontColorChooserFlyoutBase.Hide()
        Texteditor.Focus(FocusState.Programmatic)

    End Sub

    Private Sub ResetFontColor_Click(sender As Object, e As RoutedEventArgs) Handles ResetFontColor.Click

        ChangeTextFormat(
                    Function(f As ITextCharacterFormat)
                        Dim charFormatting As ITextCharacterFormat = f
                        charFormatting.ForegroundColor = Colors.Black
                        Return f
                    End Function)
        FontColorChooserFlyoutBase.Hide()
        Texteditor.Focus(FocusState.Programmatic)

    End Sub

#End Region

#Region "ParagraphFormat"

    Private parFormatStack As New List(Of ITextParagraphFormat)

    Private Sub Itemize_Click(sender As Object, e As RoutedEventArgs)
        ChangeParagraphFormat(
            Function(f As ITextParagraphFormat)
                Dim parFormatting As ITextParagraphFormat = f
                Dim toggleButton As AppBarToggleButton = DirectCast(sender, AppBarToggleButton)
                If toggleButton.IsChecked Then
                    parFormatting.ListType = MarkerType.Bullet
                    parFormatting.ListStyle = MarkerStyle.Plain
                    If parFormatting.ListLevelIndex = 0 Then
                        parFormatting.ListLevelIndex = 1
                    Else
                        parFormatStack.RemoveAt(parFormatting.ListLevelIndex - 1)
                    End If
                    parFormatStack.Add(parFormatting.GetClone())
                Else
                    parFormatting.ListStyle = MarkerStyle.NoNumber
                    parFormatting.ListLevelIndex = 0
                    parFormatStack.Clear()
                End If
                Return f
            End Function)
        ParagraphFormattingFlyout.Hide()
        Texteditor.Focus(FocusState.Programmatic)

    End Sub

    Private Sub IncreaseIndent_Click(sender As Object, e As RoutedEventArgs) Handles IncreaseIndent.Click
        ChangeParagraphFormat(
            Function(f As ITextParagraphFormat)
                Dim parFormatting As ITextParagraphFormat = f
                parFormatting.SetIndents(parFormatting.FirstLineIndent, parFormatting.LeftIndent + 40, parFormatting.RightIndent)
                If parFormatting.ListLevelIndex > 0 Then
                    parFormatting.ListLevelIndex = parFormatting.ListLevelIndex + 1
                    If parFormatting.ListType <> MarkerType.None And parFormatting.ListType <> MarkerType.Bullet Then
                        Select Case parFormatting.ListLevelIndex Mod 3
                            Case 1
                                parFormatting.ListType = MarkerType.LowercaseEnglishLetter
                            Case 2
                                parFormatting.ListType = MarkerType.LowercaseRoman
                            Case 0
                                parFormatting.ListType = MarkerType.Arabic
                        End Select
                    End If
                    parFormatStack.Add(parFormatting.GetClone())
                End If
                Return f
            End Function)
        ParagraphFormattingFlyout.Hide()
        Texteditor.Focus(FocusState.Programmatic)
    End Sub

    Private Sub DecreaseIndent_Click(sender As Object, e As RoutedEventArgs) Handles DecreaseIndent.Click
        ChangeParagraphFormat(
            Function(f As ITextParagraphFormat)
                Dim parFormatting As ITextParagraphFormat = f
                If parFormatting.ListLevelIndex > 1 Then
                    parFormatting = parFormatStack(parFormatting.ListLevelIndex - 2)
                    parFormatStack.RemoveAt(parFormatting.ListLevelIndex)
                Else
                    If parFormatting.LeftIndent > 40 Then
                        parFormatting.SetIndents(parFormatting.FirstLineIndent, parFormatting.LeftIndent - 40, parFormatting.RightIndent)
                    Else
                        parFormatting.SetIndents(parFormatting.FirstLineIndent, 0, parFormatting.RightIndent)
                    End If
                End If
                Return parFormatting
            End Function)
        ParagraphFormattingFlyout.Hide()
        Texteditor.Focus(FocusState.Programmatic)
    End Sub

    Private Sub EnumTypePalette_ItemClick(sender As Object, e As ItemClickEventArgs) Handles EnumTypePalette.ItemClick

        Dim clickedItem As EnumTypeDescriptor = DirectCast(e.ClickedItem, EnumTypeDescriptor)

        ChangeParagraphFormat(
            Function(f As ITextParagraphFormat)
                Dim parFormatting As ITextParagraphFormat = f
                parFormatting.ListType = clickedItem.Type
                parFormatting.ListStyle = clickedItem.Style
                Try
                    parFormatting.ListStart = EnumIndex.Text
                Catch ex As Exception
                End Try

                If clickedItem.Type = MarkerType.None Then
                    parFormatting.ListLevelIndex = 0
                    parFormatStack.Clear()
                Else
                    If parFormatting.ListLevelIndex = 0 Then
                        parFormatting.ListLevelIndex = 1
                    Else
                        parFormatStack.RemoveAt(parFormatting.ListLevelIndex - 1)
                    End If
                    parFormatStack.Add(parFormatting.GetClone())
                End If
                Itemize.IsChecked = False
                Return f
            End Function)
        EnumTypeFlyout.Hide()
        Texteditor.Focus(FocusState.Programmatic)

    End Sub

    Private Sub EnumTypeFlyout_Opening(sender As Object, e As Object) Handles EnumTypeFlyout.Opening

        Dim selectedText As ITextSelection = Texteditor.Document.Selection
        If selectedText IsNot Nothing Then
            Dim parFormatting As ITextParagraphFormat = selectedText.ParagraphFormat

            Dim matches = EnumTypeList.Where(Function(otherType) otherType.Type.Equals(parFormatting.ListType) And otherType.Style.Equals(parFormatting.ListStyle))
            If matches.Count() > 0 Then
                EnumTypePalette.SelectedItem = matches.First()
            End If

            If parFormatting.ListStart >= 1 Then
                EnumIndex.Text = parFormatting.ListStart.ToString()
            Else
                EnumIndex.Text = "1"
            End If
            EnumIndex.SetValue(BorderBrushProperty, New SolidColorBrush(Windows.UI.Colors.Black))
        End If

    End Sub

    Private Sub EnumTypeFlyout_Closed(sender As Object, e As Object) Handles EnumTypeFlyout.Closed
        ChangeParagraphFormat(
            Function(f As ITextParagraphFormat)
                Dim parFormatting As ITextParagraphFormat = f
                Try
                    parFormatting.ListStart = EnumIndex.Text
                Catch ex As Exception
                End Try
                Return f
            End Function)
    End Sub

    Private Sub EnumIndex_TextChanged(sender As Object, e As TextChangedEventArgs) Handles EnumIndex.TextChanged

        Try
            Dim i As Integer = EnumIndex.Text
            EnumIndex.SetValue(BorderBrushProperty, New SolidColorBrush(Windows.UI.Colors.Black))
        Catch ex As Exception
            EnumIndex.SetValue(BorderBrushProperty, New SolidColorBrush(Windows.UI.Colors.Red))
        End Try
    End Sub

    Private FirstLineIndentFlyoutBase As FlyoutBase

    Private Sub ParagraphFormattingButtons_ItemClick(sender As Object, e As ItemClickEventArgs) Handles ParagraphFormattingButtons.ItemClick

        Dim clickedItem As FrameworkElement = DirectCast(e.ClickedItem, FrameworkElement)

        Select Case clickedItem.Name
            Case "FirstLineIndent"
                Dim selectedText As ITextSelection = Texteditor.Document.Selection

                If selectedText IsNot Nothing Then
                    Dim parFormatting As ITextParagraphFormat = selectedText.ParagraphFormat
                    Select Case parFormatting.FirstLineIndent
                        Case 0
                            FirstLineIndentOptions.SelectedIndex = 0
                        Case -20
                            FirstLineIndentOptions.SelectedIndex = 1
                        Case 20
                            FirstLineIndentOptions.SelectedIndex = 2

                    End Select
                End If

                FirstLineIndentFlyoutBase = FlyoutBase.GetAttachedFlyout(clickedItem)
                FirstLineIndentFlyoutBase.ShowAt(ParagraphFormatting)
        End Select

    End Sub

    Private Sub LeaveFirstLineIndentFlyout_Click(sender As Object, e As RoutedEventArgs) Handles LeaveFirstLineIndentFlyout.Click
        ParagraphFormattingFlyout.ShowAt(ParagraphFormatting)
    End Sub

    Private Sub FirstLineIndentOptions_ItemClick(sender As Object, e As ItemClickEventArgs) Handles FirstLineIndentOptions.ItemClick

        Dim clickedItem As FrameworkElement = DirectCast(e.ClickedItem, FrameworkElement)

        Select Case clickedItem.Name
            Case "FirstLineIndentNone"
                ChangeParagraphFormat(
                    Function(f As ITextParagraphFormat)
                        Dim parFormatting As ITextParagraphFormat = f
                        parFormatting.SetIndents(0, 0, 0)
                        Return f
                    End Function)

            Case "FirstLineIndentHanging"
                ChangeParagraphFormat(
                    Function(f As ITextParagraphFormat)
                        Dim parFormatting As ITextParagraphFormat = f
                        parFormatting.SetIndents(-20, 20, 0)
                        Return f
                    End Function)

            Case "FirstLineIndentOn"
                ChangeParagraphFormat(
                    Function(f As ITextParagraphFormat)
                        Dim parFormatting As ITextParagraphFormat = f
                        parFormatting.SetIndents(20, 0, 0)
                        Return f
                    End Function)

        End Select

        FirstLineIndentFlyoutBase.Hide()
        Texteditor.Focus(FocusState.Programmatic)

    End Sub

    Private Sub AlignmentLeft_Click(sender As Object, e As RoutedEventArgs) Handles AlignmentLeft.Click
        ChangeParagraphFormat(
                    Function(f As ITextParagraphFormat)
                        Dim parFormatting As ITextParagraphFormat = f
                        parFormatting.Alignment = ParagraphAlignment.Left
                        Return f
                    End Function)
        ParagraphFormattingFlyout.Hide()
        Texteditor.Focus(FocusState.Programmatic)
    End Sub

    Private Sub AlignmentCenter_Click(sender As Object, e As RoutedEventArgs) Handles AlignmentCenter.Click
        ChangeParagraphFormat(
                    Function(f As ITextParagraphFormat)
                        Dim parFormatting As ITextParagraphFormat = f
                        parFormatting.Alignment = ParagraphAlignment.Center
                        Return f
                    End Function)
        ParagraphFormattingFlyout.Hide()
        Texteditor.Focus(FocusState.Programmatic)
    End Sub

    Private Sub AlignmentRight_Click(sender As Object, e As RoutedEventArgs) Handles AlignmentRight.Click
        ChangeParagraphFormat(
                    Function(f As ITextParagraphFormat)
                        Dim parFormatting As ITextParagraphFormat = f
                        parFormatting.Alignment = ParagraphAlignment.Right
                        Return f
                    End Function)
        ParagraphFormattingFlyout.Hide()
        Texteditor.Focus(FocusState.Programmatic)
    End Sub

    Private Sub ParagraphFormattingFlyout_Opening(sender As Object, e As Object) Handles ParagraphFormattingFlyout.Opening
        Dim selectedText As ITextSelection = Texteditor.Document.Selection
        If selectedText IsNot Nothing Then
            Dim parFormatting As ITextParagraphFormat = selectedText.ParagraphFormat
            AlignmentLeft.IsChecked = False
            AlignmentCenter.IsChecked = False
            AlignmentRight.IsChecked = False
            Select Case parFormatting.Alignment
                Case ParagraphAlignment.Left
                    AlignmentLeft.IsChecked = True
                Case ParagraphAlignment.Center
                    AlignmentCenter.IsChecked = True
                Case ParagraphAlignment.Right
                    AlignmentRight.IsChecked = True
            End Select

            ItemizeOnFlyout.IsChecked = (parFormatting.ListType = MarkerType.Bullet)
        End If
    End Sub

    Private Sub LineSpacing_SelectionChanged(sender As Object, e As SelectionChangedEventArgs) Handles LineSpacing.SelectionChanged
        ChangeParagraphFormat(
                    Function(f As ITextParagraphFormat)
                        Dim parFormatting As ITextParagraphFormat = f
                        Try
                            parFormatting.SetLineSpacing(LineSpacingRule.Multiple, LineSpacing.SelectedValue)
                        Catch ex As Exception
                        End Try
                        Return f
                    End Function)
        ParagraphFormattingFlyout.Hide()
        Texteditor.Focus(FocusState.Programmatic)
    End Sub


#End Region

#Region "FontTemplates"

    Private Sub ParagraphTemplateListView_ItemClick(sender As Object, e As ItemClickEventArgs) Handles ParagraphTemplateListView.ItemClick

        Dim clickedItem As FontFamilyDescriptor = DirectCast(e.ClickedItem, FontFamilyDescriptor)

        ChangeTextFormat(
            Function(f As ITextCharacterFormat)
                Dim charFormatting As ITextCharacterFormat = f
                charFormatting.Size = clickedItem.FontSize
                charFormatting.Name = clickedItem.FontFamilyName
                charFormatting.Weight = clickedItem.FontWeight.Weight
                charFormatting.ForegroundColor = clickedItem.ForegroundColorBrush.Color
                Return f
            End Function)

        ParagraphTemplatesFlyout.Hide()
        Texteditor.Focus(FocusState.Programmatic)
    End Sub

    Private Sub Header1_Click(sender As Object, e As RoutedEventArgs) Handles Header1.Click

        ChangeTextFormat(
            Function(f As ITextCharacterFormat)
                Dim charFormatting As ITextCharacterFormat = f
                charFormatting.Size = Header1Format.FontSize
                charFormatting.Name = Header1Format.FontFamilyName
                charFormatting.Weight = Header1Format.FontWeight.Weight
                charFormatting.ForegroundColor = Header1Format.ForegroundColorBrush.Color
                Return f
            End Function)

        Header1.IsChecked = True

        Texteditor.Focus(FocusState.Programmatic)

    End Sub

#End Region

#Region "UndoRedo"

    Private Sub Undo_Click(sender As Object, e As RoutedEventArgs) Handles Undo.Click

        Texteditor.Document.Undo()

    End Sub

    Private Sub Redo_Click(sender As Object, e As RoutedEventArgs) Handles Redo.Click

        Texteditor.Document.Redo()

    End Sub

#End Region

#Region "Image"

    Private Async Function InsertImage_ClickAsync(sender As Object, e As RoutedEventArgs) As Task Handles InsertImage.Click


        Dim openPicker = New Windows.Storage.Pickers.FileOpenPicker()
        openPicker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.PicturesLibrary
        openPicker.ViewMode = Windows.Storage.Pickers.PickerViewMode.Thumbnail

        ' Filter to include a sample subset of file types.
        openPicker.FileTypeFilter.Clear()
        openPicker.FileTypeFilter.Add(".png")
        openPicker.FileTypeFilter.Add(".jpg")

        ' Open the file picker.
        Dim file = Await openPicker.PickSingleFileAsync()

        ' file is null if user cancels the file picker.
        If file IsNot Nothing Then
            Try
                ' Open a stream for the selected file.
                Dim fileStream = Await file.OpenAsync(Windows.Storage.FileAccessMode.Read)
                ' Set the image source to the selected bitmap.
                Dim img = New Windows.UI.Xaml.Media.Imaging.BitmapImage()
                img.SetSource(fileStream)
                Texteditor.Document.Selection.InsertImage(img.PixelWidth, img.PixelHeight, 0, VerticalCharacterAlignment.Baseline, "(Image)", fileStream)
            Catch ex As Exception
            End Try
        End If


    End Function

#End Region

#Region "Timers"

    Private Sub ShowTimers_Click(sender As Object, e As RoutedEventArgs) Handles ShowTimers.Click
        TimerController.TimersPaneOpen = Not TimerController.TimersPaneOpen
    End Sub

    Private Sub ShowTimersText_Tapped(sender As Object, e As TappedRoutedEventArgs) Handles ShowTimersText.Tapped
        TimerController.TimersPaneOpen = Not TimerController.TimersPaneOpen
    End Sub

#End Region

#Region "Hamburger"
    Private Sub ToggleSplitView_Click(sender As Object, e As RoutedEventArgs) Handles ToggleSplitView.Click
        RootSplitView.IsPaneOpen = Not RootSplitView.IsPaneOpen
    End Sub
#End Region

#Region "LoadSave"
    Private Async Function LoadRecipeSource() As Task
        If CurrentRecipe.RecipeSource IsNot Nothing Then
            Try
                Dim randAccStream As Windows.Storage.Streams.IRandomAccessStream =
                  Await CurrentRecipe.RecipeSource.OpenAsync(Windows.Storage.FileAccessMode.Read)
                Texteditor.Document.LoadFromStream(Windows.UI.Text.TextSetOptions.FormatRtf, randAccStream)
                RecipeChanged = False
            Catch ex As Exception
            End Try
        End If
    End Function

    Private Async Function SaveRecipeAsync() As Task

        If CurrentRecipe.RecipeSource IsNot Nothing Then
            'Prevent updates to the remote version of the file until we 
            ' finish making changes And call CompleteUpdatesAsync.
            CachedFileManager.DeferUpdates(CurrentRecipe.RecipeSource)
            Dim randAccStream As Windows.Storage.Streams.IRandomAccessStream =
                  Await CurrentRecipe.RecipeSource.OpenAsync(Windows.Storage.FileAccessMode.ReadWrite)
            Texteditor.Document.SaveToStream(Windows.UI.Text.TextGetOptions.FormatRtf, randAccStream)
            randAccStream.Dispose()
            RecipeChanged = False

            'Let Windows know that we're finished changing the file so the 
            'other app can update the remote version of the file.
            Dim status As FileUpdateStatus = Await CachedFileManager.CompleteUpdatesAsync(CurrentRecipe.RecipeSource)
            If status <> FileUpdateStatus.Complete Then
                Dim errorBox As Windows.UI.Popups.MessageDialog = New Windows.UI.Popups.MessageDialog(App.Texts.GetString("UnableToSaveNotes"))
                Await errorBox.ShowAsync()
            End If
        End If

    End Function

    Private Async Function SaveRecipe_ClickAsync(sender As Object, e As RoutedEventArgs) As Task Handles SaveRecipe.Click
        Await SaveRecipeAsync()
    End Function

    Private Async Sub SaveRecipeText_TappedAsync(sender As Object, e As TappedRoutedEventArgs) Handles SaveRecipeText.Tapped
        Await SaveRecipeAsync()
    End Sub

    Enum UserChoice
        yes
        no
        cancel
    End Enum

    Private Async Sub LeaveEditor_ClickAsync(sender As Object, e As RoutedEventArgs) Handles LeaveEditor.Click
        If Await TryLeaveEditorAsync() Then
            NavigationHelper.GoBack()
        End If
    End Sub

    'Private Async Sub LeaveEditorText_TappedAsync(sender As Object, e As TappedRoutedEventArgs) Handles LeaveEditorText.Tapped
    '    If Await TryLeaveEditorAsync() Then
    '        NavigationHelper.GoBack()
    '    End If
    'End Sub

    Private Async Function TryLeaveEditorAsync() As Task(Of Boolean)

        If RecipeChanged Then
            Dim SaveDialog As New SaveChangesDialog
            Await SaveDialog.ShowAsync()
            Select Case SaveDialog.UserChoice
                Case SaveChangesDialog.UserChoices.Yes
                    Await SaveRecipeAsync()
                    Return True

                Case SaveChangesDialog.UserChoices.No
                    RecipeChanged = False
                    Return True

                Case SaveChangesDialog.UserChoices.Cancel
                    Return False
            End Select
        End If

        Return True

    End Function

#End Region

#Region "Help"
    Private Async Sub AppHelp_ClickAsync(sender As Object, e As RoutedEventArgs) Handles AppHelp.Click
        RootSplitView.IsPaneOpen = False
        If Await TryLeaveEditorAsync() Then
            Me.Frame.Navigate(GetType(RecipesPage), HelpDocuments.FolderName)
        End If
    End Sub

    Private Async Sub AppHelpText_TappedAsync(sender As Object, e As TappedRoutedEventArgs) Handles AppHelpText.Tapped
        RootSplitView.IsPaneOpen = False
        If Await TryLeaveEditorAsync() Then
            Me.Frame.Navigate(GetType(RecipesPage), HelpDocuments.FolderName)
        End If
    End Sub

#End Region

End Class

