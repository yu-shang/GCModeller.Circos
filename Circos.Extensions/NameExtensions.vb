﻿Imports LANS.SystemsBiology.AnalysisTools.DataVisualization.Interaction.Circos.Documents.Karyotype.Highlights
Imports LANS.SystemsBiology.AnalysisTools.DataVisualization.Interaction.Circos.Documents.Karyotype.Highlights.LocusLabels
Imports LANS.SystemsBiology.Assembly.NCBI.GenBank
Imports LANS.SystemsBiology.Assembly.NCBI.GenBank.TabularFormat
Imports LANS.SystemsBiology.Assembly.NCBI.GenBank.TabularFormat.ComponentModels
Imports LANS.SystemsBiology.DatabaseServices.Regprecise
Imports Microsoft.VisualBasic.Linq

Public Module NameExtensions
    ''' <summary>
    ''' 生成Regulon的调控因子的名称预处理数据
    ''' </summary>
    ''' <param name="DIR"></param>
    ''' <param name="gb"></param>
    ''' <returns></returns>
    Public Function DumpNames(DIR As String, gb As PTT) As Name()
        Dim genes = __loadCommon(DIR, gb)
        Return genes.ToArray(Function(x) New Name With {
                                 .Name = $"[{x.Key}]  {x.Value.Product}",
                                 .Maximum = CInt(x.Value.Location.Right),
                                 .Minimum = CInt(x.Value.Location.Left)})
    End Function

    Private Function __loadCommon(DIR As String, gb As PTT) As KeyValuePair(Of String, GeneBrief)()
        Dim bbh = (From file As String
                   In FileIO.FileSystem.GetFiles(DIR, FileIO.SearchOption.SearchTopLevelOnly, "*.xml").AsParallel
                   Select file.LoadXml(Of BacteriaGenome).Regulons.Regulators).ToArray.MatrixToList
        Dim Maps = gb.LocusMaps
        Dim regLocus = (From x In bbh Select New With {.Family = x.Family, .Locus = x.LocusId}).ToArray

        For i As Integer = 0 To regLocus.Length - 1
            Dim s As String = regLocus(i).Locus

            If Maps.ContainsKey(s) Then
                regLocus(i).Locus = Maps(s)
            End If
        Next

        Dim g = (From s In regLocus
                 Where gb.ExistsLocusId(s.Locus)
                 Select gg = gb(s.Locus),
                     s.Locus,
                     s
                 Group By Locus Into Group) _
                    .ToArray(Function(x) New KeyValuePair(Of String, GeneBrief)(
                          x.Group.ToArray(Function(og) og.s.Family).Distinct.JoinBy("; "),
                          x.Group.First.gg))
        Return g
    End Function

    Public Function RegulonRegulators(DIR As String, gb As PTT) As HighLightsMeta()
        Dim g = __loadCommon(DIR, gb).ToArray(Function(x) x.Value)
        Dim list = g.ToArray(Function(x) New Name With {
                                 .Name = x.Product,
                                 .Maximum = CInt(x.Location.Right),
                                 .Minimum = CInt(x.Location.Left)})
        Return list.ToArray(Function(x) x.ToMeta)
    End Function
End Module