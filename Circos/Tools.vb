﻿Module Tools

    Public ReadOnly Property currentDir As String =
        FileIO.FileSystem.CurrentDirectory.Replace("\", "/") & "/"

    Public Function TrimPath(doc As Documents.Configurations.ConfigDoc) As String
        If doc.IsSystemConfig Then
            Return doc.FilePath
        End If

        Dim url As String = doc.FilePath
        Return TrimPath(url)
    End Function

    Public Function TrimPath(url As String) As String
        Dim refPath As String = url.Replace("\", "/").Replace(currentDir, "")
        Return refPath
    End Function

End Module