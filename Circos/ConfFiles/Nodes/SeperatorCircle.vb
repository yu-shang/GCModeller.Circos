﻿#Region "Microsoft.VisualBasic::f834225a2273fe4660a624bb9b315127, ..\interops\visualize\Circos\Circos\ConfFiles\Nodes\SeperatorCircle.vb"

    ' Author:
    ' 
    '       asuka (amethyst.asuka@gcmodeller.org)
    '       xieguigang (xie.guigang@live.com)
    ' 
    ' Copyright (c) 2016 GPL3 Licensed
    ' 
    ' 
    ' GNU GENERAL PUBLIC LICENSE (GPL3)
    ' 
    ' This program is free software: you can redistribute it and/or modify
    ' it under the terms of the GNU General Public License as published by
    ' the Free Software Foundation, either version 3 of the License, or
    ' (at your option) any later version.
    ' 
    ' This program is distributed in the hope that it will be useful,
    ' but WITHOUT ANY WARRANTY; without even the implied warranty of
    ' MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    ' GNU General Public License for more details.
    ' 
    ' You should have received a copy of the GNU General Public License
    ' along with this program. If not, see <http://www.gnu.org/licenses/>.

#End Region

Imports System.Text
Imports SMRUCC.genomics.Visualize.Circos.TrackDatas
Imports Microsoft.VisualBasic.ComponentModel.Settings

Namespace Configurations.Nodes.Plots

    Public Class SeperatorCircle : Inherits Histogram

        Dim Width As Integer

        Sub New(Length As Integer, Width As Integer)
            Call MyBase.New(data:=NtProps.GCSkew.CreateLineData(Length, Width))
            Me.Width = Width
        End Sub
    End Class
End Namespace
