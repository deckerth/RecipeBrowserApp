Imports RecipeBrowser.Persistency
Imports Windows.UI

Public Class TagRepository

    Private Shared _current As TagRepository
    Public Shared ReadOnly Property Current As TagRepository
        Get
            If _current Is Nothing Then
                _current = New TagRepository()
            End If
            Return _current
        End Get
    End Property

    Public Sub New()
        _current = Me
    End Sub

    Private _directory As List(Of TagDirectory)
    Public ReadOnly Property Directory As List(Of TagDirectory)
        Get
            If _directory Is Nothing Then
                _directory = MetaDataDatabase.Current.GetTagDirectory()
            End If
            Return _directory
        End Get
    End Property

    Public Function GetTag(aTag As String) As TagDirectory
        Return Directory.Where(Function(otherTag) otherTag.Tag.Equals(aTag)).FirstOrDefault()
    End Function

    Public Sub AddTag(newTag As String, Background As Color, Foreground As Color)
        Dim inst As New TagDirectory(newTag, Background, Foreground)
        _directory.Add(inst)
        MetaDataDatabase.Current.AddTag(inst)
    End Sub

    Public Sub DeleteTag(toDelete As String)
        Dim inst = GetTag(toDelete)
        If inst IsNot Nothing Then
            _directory.Remove(inst)
            MetaDataDatabase.Current.DeleteTag(toDelete)
        End If
    End Sub

    Public Function TagsAreEqual(tagName1 As String, tag2 As TagDirectory)
        Dim inst = GetTag(tagName1)
        If inst IsNot Nothing Then
            Return inst.Tag.Equals(tag2.Tag) AndAlso
               inst.GetForeground() = tag2.GetForeground() AndAlso
               inst.GetBackground() = tag2.GetBackground()
        Else
            Return False
        End If
    End Function

    Public Sub RenameTag(oldTag As String, newTag As TagDirectory)
        Dim inst = GetTag(oldTag)
        If inst IsNot Nothing Then
            If inst.Tag.Equals(newTag.Tag) AndAlso
               inst.GetForeground() = newTag.GetForeground() AndAlso
               inst.GetBackground() = newTag.GetBackground() Then
                Return
            End If
            inst.Tag = newTag.Tag
            inst.SetBackground(newTag.GetBackground())
            inst.SetForeground(newTag.GetForeground())
            MetaDataDatabase.Current.RenameTag(oldTag, newTag)
        End If
    End Sub

End Class
