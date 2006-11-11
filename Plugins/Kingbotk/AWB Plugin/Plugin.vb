' We might want to load the article and check it's not a redirect, not redlinked, what categories it is in etc
' (If so, should probably do before processing the talk page, and should probably be in base class)

' TODO: Don't skip if contains an **empty** importance=param. Replace it with priority=.
' TODO: Should priority be placed immediately after class, instead of at the end?
' TODO: replace {{Infoboxneeded}} with needs-infobox (and for albums and songs do same)
' TODO: regexp changes/error catching (perhaps could run a second less strict regex) - problems: _
'    invalid parms e.g. needs=infobox=yes (skip and log), [[Talk:John Stevens (hockey)]] - {{WPBiography|living=yes|}} (ditto)-->

Namespace AWB.Plugins.SDKSoftware.Kingbotk

    ''' <summary>
    ''' SDK Software's base class for template-manipulating AWB plugins
    ''' </summary>
    Public MustInherit Class SDKAWBTemplatesPluginBase
        ' what about <nowiki> and <pre>?? hmm

        ' AWB objects:
        Protected Shared webcontrol As WikiFunctions.Browser.WebControl, contextmenu As ContextMenuStrip, _
           listmaker As WikiFunctions.Lists.ListMaker, tabcontrol As TabControl
        Protected WithEvents OurMenuItem As ToolStripMenuItem

        ' Library state and shared objects:
        Private Shared Initialiased As Boolean
        Private Shared WithEvents MenuAbout As New ToolStripMenuItem("About the kingbotk plugin")
        Private Shared WithEvents MenuHelp As New ToolStripMenuItem("Help for the kingbotk plugin")
        Protected Shared PluginsTab As New TabPage("Kingbotk")
        Private Shared AWBOptionsTab As TabPage
        Private Shared AWBMoreOptionsTab As TabPage
        Private Shared AWBStartTab As TabPage
        Private Shared PluginsTabUserControl As New PluginSettings
        Protected Shared SettingsTabs As New List(Of TabPage)
        Private Shared ActivePlugins As New List(Of SDKAWBTemplatesPluginBase)

        ' Article-edit state:
        Protected Article As Article

        ' Regular expression
        Protected regexp As Regex
        Protected Const conRegexpLeft As String = "\{\{\s*(?<tl>template\s*:)?\s*(?<tlname>"
        Protected Const conRegexpRight As String = _
           ")[\s\n\r]*(([\s\n\r]*\|[\s\n\r]*(?<parm>[-a-z0-9&]*)[\s\n\r]*)+(=[\s\n\r]*(?<val>[-a-z0-9]*)[\s\n\r]*)?)*\}\}[\s\n\r]*"
        Protected Const regexpoptions As RegexOptions = _
           RegexOptions.Compiled Or RegexOptions.Multiline Or RegexOptions.IgnoreCase Or RegexOptions.ExplicitCapture

        Protected Sub New()
        End Sub

        Protected Overridable Sub Initialise(ByVal list As WikiFunctions.Lists.ListMaker, _
        ByVal web As WikiFunctions.Browser.WebControl, ByVal tsmi As ToolStripMenuItem, _
        ByVal cms As ContextMenuStrip, ByVal tab As TabControl)
            ' Set us up globally and store references to AWB objects:
            If Not Initialiased Then StartupInit(list, web, tsmi, cms, tab)

            ' Set up general tab:
            PluginsTab.UseVisualStyleBackColor = True
            PluginsTab.Controls.Add(PluginsTabUserControl)

            ' Set up menu item:
            InitMenuItem() ' runs in descendent classes
            With OurMenuItem
                .CheckOnClick = True
                .Checked = False
            End With
            tsmi.DropDownItems.Add(OurMenuItem)
        End Sub
        Private Sub StartupInit(ByVal list As WikiFunctions.Lists.ListMaker, _
        ByVal web As WikiFunctions.Browser.WebControl, ByVal tsmi As System.Windows.Forms.ToolStripMenuItem, _
        ByVal cms As System.Windows.Forms.ContextMenuStrip, ByVal tab As System.Windows.Forms.TabControl)
            Initialiased = True
            webcontrol = Web
            contextmenu = cms
            listmaker = List
            tabcontrol = tab
            DirectCast(tsmi.Owner.Items("helpToolStripMenuItem"), ToolStripMenuItem).DropDownItems.AddRange( _
               {MenuAbout, MenuHelp})
            'SettingsTabs.AddRange({tab.TabPages("tpSetOptions"), tab.TabPages("tpMoreOptions"), tab.TabPages("tpStart")})
            AWBMoreOptionsTab = tab.TabPages("tpMoreOptions")
            AWBOptionsTab = tab.TabPages("tpSetOptions")
            AWBStartTab = tab.TabPages("tpStart")
            With PluginsTabUserControl
                AddHandler .btnDiff.Click, AddressOf Me.ButtonClickEventHander
                AddHandler .btnStop.Click, AddressOf Me.ButtonClickEventHander
                AddHandler .btnStart.Click, AddressOf Me.ButtonClickEventHander
                AddHandler .btnPreview.Click, AddressOf Me.ButtonClickEventHander
                AddHandler .btnSave.Click, AddressOf Me.ButtonClickEventHander
                AddHandler .btnIgnore.Click, AddressOf Me.ButtonClickEventHander
                .Controls.Add(AWBStartTab.Controls("groupBox3"))
                .Controls("groupBox3").BringToFront()
            End With
        End Sub

        Protected Function CheckArticleForProcessing(ByVal ArticleTitle As String, ByRef Skip As Boolean, _
        ByVal [Namespace] As Integer, ByVal AllowProject As Boolean, ByVal AllowMain As Boolean, _
        ByVal AllowAllTalk As Boolean) As Boolean
            If Not OurMenuItem.Checked Then
                Return False
            ElseIf Not ValidNamespace(DirectCast([Namespace], Namespaces), AllowProject, AllowMain, AllowAllTalk) Then
                ' TODO: Log skipped articles
                Skip = True
                Return False
            Else
                Return True
            End If
        End Function

        Protected Function FindTemplate(ByVal ArticleText As String, ByVal PreferredTemplateNameRegex As Regex, _
        ByVal PreferredTemplateNameWiki As String, ByVal PluginWikiShortcut As String, ByVal JobSummary As String) As String
            Article = New Article(PreferredTemplateNameRegex, PreferredTemplateNameWiki, PluginWikiShortcut, JobSummary)

            FindTemplate = regexp.Replace(ArticleText, AddressOf Me.MatchEvaluator)

            ' Pass over to inherited classes to do their specific jobs:
            FindTemplate = ParseArticle(FindTemplate)

            Article.FinaliseEditSummary(webcontrol)
        End Function

        Protected Function MatchEvaluator(ByVal match As Match) As String
            If Not match.Groups("parm").Captures.Count = match.Groups("val").Captures.Count Then
                MessageBox.Show("Parms and val don't match")
                Throw New Exception("Bug? Parameters and value count don't match")
            End If

            Article.FoundTemplate = True
            Article.CheckTemplateCall(match.Groups("tl").Value)
            Article.CheckTemplateName(match.Groups("tlname").Value)

            If match.Groups("parm").Captures.Count > 0 Then
                For i As Integer = 0 To match.Groups("parm").Captures.Count - 1
                    'Str += "Parm " & i.ToString & ": " & match.Groups("parm").Captures(i).Value & " = /" & _
                    '   match.Groups("val").Captures(i).Value & "/" & microsoft.visualbasic.vbcrlf
                    If Not match.Groups("val").Captures(i).Value = "" Then
                        With match.Groups("parm").Captures(i)
                            Article.AddTemplateParm(New Article.TemplateParametersObject( _
                               match.Groups("parm").Captures(i).Value, match.Groups("val").Captures(i).Value))
                        End With
                    End If
                Next
            End If

            Return "" ' Always return an empty string; if we don't skip we'll add our own template instance
        End Function

        Protected Function TNF(ByVal ArticleText As String) As String
            Article.Major = True
            Article.Skip = False
            Return TemplateNotFound(ArticleText)
        End Function

        Protected Function ValidNamespace(ByVal Nmespace As Namespaces, ByVal AllowProject As Boolean, _
        ByVal AllowMain As Boolean, ByVal AllowAllTalk As Boolean) As Boolean
            Select Case Nmespace
                Case Namespaces.Talk
                    Return True
                Case Namespaces.Main
                    Return AllowMain
                Case Namespaces.Project
                    Return AllowProject
                Case Namespaces.ProjectTalk, Namespaces.PortalTalk, Namespaces.CategoryTalk, Namespaces.UserTalk, _
                Namespaces.HelpTalk, Namespaces.ImageTalk, Namespaces.MediawikiTalk
                    Return AllowAllTalk
            End Select
        End Function

        Protected MustOverride Sub InitMenuItem()
        Protected MustOverride Function ParseArticle(ByVal ArticleText As String) As String
        Protected MustOverride Function TemplateNotFound(ByVal ArticleText As String) As String
        Protected MustOverride Sub AWBStop()
        Protected MustOverride Sub AWBStart()
        Protected MustOverride Sub AWBPreview()
        Protected MustOverride Sub AWBSave()
        Protected MustOverride Sub AWBDiff()
        Protected MustOverride Sub AWBIgnore()

        Protected MustOverride Sub ShowHideOurObjects(ByVal Visible As Boolean)

        Friend Shared Sub HideTabs()
            tabcontrol.TabPages.Remove(AWBOptionsTab)
            tabcontrol.TabPages.Remove(AWBMoreOptionsTab)
            tabcontrol.TabPages.Remove(AWBStartTab)
            For Each tabp As TabPage In SettingsTabs
                tabcontrol.TabPages.Remove(tabp)
            Next
        End Sub

        Friend Property Enabled() As Boolean ' NOT shared, this is per plugin
            Get
                Return OurMenuItem.Checked
            End Get
            Set(ByVal IsEnabled As Boolean)
                'If Not OurMenuItem.Checked = IsEnabled Then ' THIS DIDN'T WORK, SO NEED TO BE CAREFUL WHEN SETTING FROM XML
                OurMenuItem.Checked = IsEnabled
                ShowHideOurObjects(IsEnabled)
                If IsEnabled Then
                    ActivePlugins.Add(Me)
                    If ActivePlugins.Count = 1 Then tabcontrol.TabPages.Add(PluginsTab)
                Else
                    ActivePlugins.Remove(Me)
                    If ActivePlugins.Count = 0 Then tabcontrol.TabPages.Remove(PluginsTab)
                End If
                'End If
            End Set
        End Property

        Private Shared Sub MenuAbout_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles MenuAbout.Click
            Dim about As New AboutBox(String.Format("Version {0}", _
               System.Reflection.Assembly.GetExecutingAssembly.GetName.Version.ToString))
            about.Show()
        End Sub
        Private Shared Sub MenuHelp_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles MenuHelp.Click
            System.Diagnostics.Process.Start("http://en.wikipedia.org/wiki/User:Kingbotk/Plugin")
        End Sub
        Private Sub ourmenuitem_CheckedChanged(ByVal sender As Object, ByVal e As System.EventArgs) _
        Handles OurMenuItem.CheckedChanged
            Enabled = OurMenuItem.Checked
        End Sub

        Private Sub ButtonClickEventHander(ByVal sender As System.Object, ByVal e As System.EventArgs)
            Dim btn As Button = DirectCast(sender, Button)
            Select Case btn.Name
                Case "btnStop"
                    AWBStop()
                Case "btnStart"
                    AWBStart()
                Case "btnPreview"
                    AWBPreview()
                Case "btnSave"
                    AWBSave()
                Case "btnDiff"
                    AWBDiff()
                Case "btnIgnore"
                    AWBIgnore()
            End Select
        End Sub
    End Class

    ''' <summary>
    ''' An AWB plugin which ensures that a talk page contains WPBiography|living=yes
    ''' </summary>
    Public NotInheritable Class WPBiographyPlugin
        Inherits SDKAWBTemplatesPluginBase
        Implements IAWBPlugin

        Private Const conPluginName As String = "WPBiography Plugin"

        ' Regular expressions:
        Private TemplateNameRegex As New Regex("[Ww]PBiography", RegexOptions.Compiled)
        Private BLPRegex As New Regex("\{\{\s*(template\s*:\s*|)\s*blp\s*\}\}[\s\n\r]*", _
           RegexOptions.IgnoreCase Or RegexOptions.Compiled Or RegexOptions.ExplicitCapture)
        Private SkipRegex As New Regex("WPBeatles|\{\{KLF", RegexOptions.IgnoreCase Or RegexOptions.Compiled)

        ' Settings:
        Private OurTab As New TabPage("WPBiography")
        Private WithEvents OurSettingsControl As New WPBioSettings
        'Protected XMLSettings As Components.XMLSettings ' plugins which don't have a tab can use this interface seperately

        Public Event Diff() Implements WikiFunctions.Plugin.IAWBPlugin.Diff
        Public Event Preview() Implements WikiFunctions.Plugin.IAWBPlugin.Preview
        Public Event Save() Implements WikiFunctions.Plugin.IAWBPlugin.Save
        Public Event Skip() Implements WikiFunctions.Plugin.IAWBPlugin.Skip
        Public Event Start() Implements WikiFunctions.Plugin.IAWBPlugin.Start
        Public Event [Stop]() Implements WikiFunctions.Plugin.IAWBPlugin.Stop

        Public Sub New()
            MyBase.New()
            regexp = New Regex(conRegexpLeft & "WPBiography|BioWikiProject" & conRegexpRight, regexpoptions)
            ' could do this in a renamed InitMenuItem() or in the base class by using a protected var for the template
            ' names. This way is fine for now :)
        End Sub

        Public Shadows Sub Initialise(ByVal list As WikiFunctions.Lists.ListMaker, ByVal web As WikiFunctions.Browser.WebControl, _
        ByVal tsmi As ToolStripMenuItem, ByVal cms As ContextMenuStrip, ByVal tab As TabControl, ByVal frm As Form, _
        ByVal txt As TextBox) Implements WikiFunctions.Plugin.IAWBPlugin.Initialise
            MyBase.Initialise(list, web, tsmi, cms, tab)
            OurTab.UseVisualStyleBackColor = True
            OurTab.Controls.Add(OurSettingsControl)
        End Sub

        Protected Overrides Sub InitMenuItem()
            OurMenuItem = New ToolStripMenuItem(conPluginName)
            SettingsTabs.Add(OurTab)
            'ourmenuitem.DropDownItems.Add(New ToolStripTextBox)
        End Sub

        Public ReadOnly Property Name() As String Implements WikiFunctions.Plugin.IAWBPlugin.Name
            Get
                Return conPluginName
            End Get
        End Property

        Public Function ProcessArticle(ByVal ArticleText As String, ByVal ArticleTitle As String, _
        ByVal [Namespace] As Integer, ByRef Summary As String, ByRef Skip As Boolean) As String _
        Implements WikiFunctions.Plugin.IAWBPlugin.ProcessArticle
            ' TODO: these constants were formerly arguments and will probably need to be refined e.g. to user options
            Const JobSummary As String = _
               "Tag [[Category:Living people]] articles with {{[[Template:WPBiography|WPBiography]]}}. "
            Const ForceAddition As Boolean = True, AllowProject As Boolean = False, AllowAllTalk As Boolean = False, _
            AllowMain As Boolean = False

            If Not CheckArticleForProcessing(ArticleTitle, Skip, [Namespace], AllowProject, AllowMain, AllowAllTalk) Then
                Return ArticleText
            End If

            ' Skip if contains {{WPBeatles}} or {{KLF}}
            If SkipRegex.Matches(ArticleText).Count > 0 Then
                Skip = True
                Return ArticleText
            End If

            ' Look for the template and parameters
            ProcessArticle = MyBase.FindTemplate(ArticleText, TemplateNameRegex, "WPBiography", _
               "BLP", JobSummary)
            Summary = Article.EditSummary
            Skip = Article.Skip

            With Article
                If (Not .FoundTemplate) And ForceAddition Then
                    ArticleText = TNF(ProcessArticle)
                    Skip = False
                ElseIf Not Skip Then
                    ArticleText = "{{WPBiography" & Microsoft.VisualBasic.vbCrLf
                    If .TemplateParameters.ContainsKey("living") Then
                        ArticleText += "|living=" + .TemplateParameters("living").Value + _
                           Microsoft.VisualBasic.vbCrLf
                    End If
                    ArticleText += "|class="
                    If .TemplateParameters.ContainsKey("class") Then
                        ArticleText += .TemplateParameters("class").Value
                    End If
                    ArticleText += Microsoft.VisualBasic.vbCrLf

                    For Each o As KeyValuePair(Of String, Article.TemplateParametersObject) In .TemplateParameters
                        With o
                            Select Case .Key
                                Case "living"
                                Case "class"
                                    Exit Select
                                Case Else
                                    ArticleText += "|" + .Key + "=" + .Value.Value + Microsoft.VisualBasic.vbCrLf
                            End Select
                        End With
                    Next

                    ArticleText += "}}" + Microsoft.VisualBasic.vbCrLf + ProcessArticle
                End If
            End With

            Return ArticleText
        End Function

        Protected Overrides Function ParseArticle(ByVal ArticleText As String) As String
            ' The base classes have got any template parms, now we need to do our bit
            Return ParseBioArticle(True, ArticleText)
            ' Anything specific to this plugin can follow here:
        End Function
        Private Function ParseBioArticle(ByVal Living As Boolean, ByVal ArticleText As String) As String
            ' The plugin base class has got any template parms, now we need to do our bit (generic WPBio jobs)
            ' TODO: This can be merged into ParseArticle() and Living replaced with option
            With Article
                If .FoundTemplate Then
                    If .TemplateParameters.ContainsKey("importance") Then
                        If .TemplateParameters.ContainsKey("priority") Then
                            .EditSummary += "rm importance param, has priority=, "
                        Else
                            .TemplateParameters.Add("priority", _
                               New Article.TemplateParametersObject("priority", .TemplateParameters("importance").Value))
                            .EditSummary += "importance→priority, "
                        End If
                        .TemplateParameters.Remove("importance")
                        .Skip = False
                    End If
                End If

                ' this is based on PerformFindAndReplace() from AWB, and could be turned into a function if need be
                If (BLPRegex.Matches(ArticleText).Count > 0) Then
                    ArticleText = BLPRegex.Replace(ArticleText, "")
                    .EditSummary += "{{[[Template:Blp|Blp]]}}→living=yes, "
                    Living = True
                    .Skip = False
                End If

                If Living Then
                    If .TemplateParameters.ContainsKey("living") Then
                        If Not .TemplateParameters("living").Value = "yes" Then
                            .TemplateParameters("living").Value = "yes"
                            .Skip = False
                            .Major = True
                            ' if living=yes then no change needed here
                        End If
                    Else
                        .TemplateParameters.Add("living", New Article.TemplateParametersObject("living", "yes"))
                        .Skip = False
                        .Major = True
                    End If
                End If
            End With

            Return ArticleText
        End Function

        Protected Overrides Function TemplateNotFound(ByVal ArticleText As String) As String
            Return "{{WPBiography" & Microsoft.VisualBasic.vbCrLf & "|living=yes" & _
               Microsoft.VisualBasic.vbCrLf & "|class=" & Microsoft.VisualBasic.vbCrLf & "}}" & _
               Microsoft.VisualBasic.vbCrLf & ArticleText
        End Function

        Public Sub ReadXML(ByVal Reader As System.Xml.XmlTextReader) Implements WikiFunctions.Plugin.IAWBPlugin.ReadXML
            OurSettingsControl.ReadXML(Reader)
        End Sub
        Public Sub Reset() Implements WikiFunctions.Plugin.IAWBPlugin.Reset
            OurSettingsControl.Reset()
        End Sub
        Public Sub WriteXML(ByVal Writer As System.Xml.XmlTextWriter) Implements WikiFunctions.Plugin.IAWBPlugin.WriteXML
            OurSettingsControl.WriteXML(Writer)
        End Sub

        Protected Overrides Sub ShowHideOurObjects(ByVal Visible As Boolean)
            If Visible Then
                tabcontrol.TabPages.Add(OurTab) ' DirectCast(ourtab, TabPage))
            Else
                tabcontrol.TabPages.Remove(OurTab) '(DirectCast(ourtab, TabPage))
            End If
        End Sub

        ' AWB event handlers (something of an unnecessary liability, as AWB communicates with each plugin as though it's standalone)
        Protected Overrides Sub AWBDiff()
            RaiseEvent Diff()
        End Sub
        Protected Overrides Sub AWBIgnore()
            RaiseEvent Skip()
        End Sub
        Protected Overrides Sub AWBPreview()
            RaiseEvent Preview()
        End Sub
        Protected Overrides Sub AWBSave()
            RaiseEvent Save()
        End Sub
        Protected Overrides Sub AWBStart()
            RaiseEvent Start()
        End Sub
        Protected Overrides Sub AWBStop()
            RaiseEvent [Stop]()
        End Sub
    End Class
End Namespace