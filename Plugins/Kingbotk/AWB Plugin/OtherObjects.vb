Namespace AWB.Plugins.SDKSoftware.Kingbotk
    ''' <summary>
    ''' An object representing an article which may or may not contain the targetted template
    ''' </summary>
    Public Class Article
        Friend Skip As Boolean = True
        Friend FoundTemplate As Boolean = False, Major As Boolean = False
        Friend EditSummary As String = "([[User:Kingbotk#FAQ|FAQ]], [[User:Kingbotk/" ' save the first bracket in the AWB edit summary box, so it doesn't complain
        ' length=61, maxlength=200, remaining=139
        Friend TemplateParameters As New Dictionary(Of String, TemplateParametersObject)
        Private PreferredTemplateNameRegex As Regex, PreferredTemplateNameWiki As String

        Friend Sub FinaliseEditSummary(ByVal webcontrol As WikiFunctions.Browser.WebControl)
            If Skip Then
                EditSummary = "This article should have been skipped"
            Else
                EditSummary = Regex.Replace(EditSummary, ", $", ".")
                webcontrol.SetMinor(Not Major)
            End If
        End Sub
        Friend Sub CheckTemplateCall(ByVal TemplateCall As String)
            If Not TemplateCall = "" Then ' we have "template:"
                Skip = False
                EditSummary += "Remove ""template:"", "
            End If
        End Sub
        Friend Sub CheckTemplateName(ByVal TemplateName As String)
            If Not PreferredTemplateNameRegex.Match(TemplateName).Success Then
                Skip = False
                EditSummary += TemplateName + "→" + PreferredTemplateNameWiki + ", "
            End If
        End Sub
        Friend Sub AddTemplateParm(ByVal NewParm As TemplateParametersObject)
            On Error Resume Next
            ' TODO: Can we make this more robust for duplicate parms? Or is there no answer to such things?
            TemplateParameters.Add(NewParm.Name, NewParm)
        End Sub

        Friend Class TemplateParametersObject
            Public Name As String
            Public Value As String

            Friend Sub New(ByVal ParameterName As String, ByVal ParameterValue As String)
                Name = ParameterName
                Value = ParameterValue
            End Sub
        End Class

        Friend Sub New(ByVal objPreferredTemplateName As Regex, ByVal strPreferredTemplateNameWiki As String, _
        ByVal PluginWikiShortcut As String, ByVal JobSummary As String)
            PreferredTemplateNameRegex = objPreferredTemplateName
            PreferredTemplateNameWiki = strPreferredTemplateNameWiki
            EditSummary += PluginWikiShortcut + "|Plugin]]) " + JobSummary
        End Sub
    End Class
End Namespace

Namespace AWB.Plugins.SDKSoftware.Kingbotk.Components
    Public Interface XMLSettings
        Sub ReadXML(ByVal Reader As System.Xml.XmlTextReader)
        Sub Reset()
        Sub WriteXML(ByVal Writer As System.Xml.XmlTextWriter)
    End Interface

    Friend Module XMLUtils
        Friend Function XMLReadBoolean(ByVal reader As System.Xml.XmlTextReader, ByVal param As String) As Boolean
            reader.MoveToAttribute(param)
            Return Boolean.Parse(reader.Value)
        End Function
        Friend Function XMLReadString(ByVal reader As System.Xml.XmlTextReader, ByVal param As String) As String
            reader.MoveToAttribute(param)
            Return reader.Value
        End Function
    End Module
End Namespace