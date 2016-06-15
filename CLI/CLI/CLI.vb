﻿Imports LANS.SystemsBiology.AnalysisTools.DataVisualization.Interaction
Imports LANS.SystemsBiology.AnalysisTools.DataVisualization.Interaction.Circos
Imports LANS.SystemsBiology.AnalysisTools.DataVisualization.Interaction.Circos.Documents.Configurations.Nodes
Imports LANS.SystemsBiology.AnalysisTools.DataVisualization.Interaction.Circos.Documents.Karyotype.Highlights
Imports LANS.SystemsBiology.Assembly.NCBI.GenBank
Imports LANS.SystemsBiology.NCBI.Extensions.LocalBLAST.Application.RpsBLAST
Imports LANS.SystemsBiology.NCBI.Extensions.NCBIBlastResult
Imports LANS.SystemsBiology.SequenceModel.FASTA
Imports LANS.SystemsBiology.SequenceModel.NucleotideModels
Imports LANS.SystemsBiology.SequenceModel.NucleotideModels.NucleicAcidStaticsProperty
Imports LANS.SystemsBiology.AnalysisTools.DataVisualization.Interaction.Circos.Documents.Karyotype
Imports LANS.SystemsBiology
Imports LANS.SystemsBiology.SequenceModel.Patterns
Imports Microsoft.VisualBasic.CommandLine.Reflection
Imports Microsoft.VisualBasic.DocumentFormat.Csv
Imports Microsoft.VisualBasic.DocumentFormat.Csv.StorageProvider.Reflection
Imports Microsoft.VisualBasic.Scripting.MetaData
Imports Microsoft.VisualBasic.Linq.Extensions

<PackageNamespace("Circos.CLI",
                  Category:=APICategories.CLI_MAN,
                  Description:="Tools for generates the circos drawing model file for the circos perl script.")>
Public Module CLI

    <ExportAPI("/NT.Variation",
               Usage:="/NT.Variation /mla <fasta.fa> [/ref <index/fasta.fa, 0> /out <out.txt> /cut 0.75]")>
    Public Function NTVariation(args As CommandLine.CommandLine) As Integer
        Dim mla As String = args("/mla")
        Dim ref As String = args("/ref")
        Dim cut As Double = args.GetValue("/cut", 0.75)
        Dim source As FastaFile = New FastaFile(mla)

        If ref.FileExists Then
            Dim refFa As FastaToken = New FastaToken(ref)
            Dim out As String = args.GetValue("/out", mla.TrimFileExt & "-" & ref.BaseName & ".NTVariations.txt")
            Dim vec = refFa.NTVariations(source, cut)
            Return vec.FlushAllLines(out).CLICode
        Else
            Dim idx As Integer = Scripting.CastInteger(ref)
            Dim out As String = args.GetValue("/out", mla.TrimFileExt & "." & idx & ".NTVariations.txt")
            Dim vec = NTVariations(source, idx, cut)
            Return vec.FlushAllLines(out).CLICode
        End If
    End Function

    <ExportAPI("--AT.Percent", Usage:="--AT.Percent /in <in.fasta> [/win_size <200> /step <25> /out <out.txt>]")>
    Public Function ATContent(args As CommandLine.CommandLine) As Integer
        Dim inFasta As String = args("/in")
        Dim out As String = args.GetValue("/out", inFasta.TrimFileExt & ".ATPercent.txt")
        Dim winSize As Integer = args.GetValue("/win_size", 200)
        Dim steps As Integer = args.GetValue("/step", 25)
        Dim lst As FastaFile = FastaFile.Read(inFasta)

        If lst.NumberOfFasta = 1 Then
            Return __propertyVector(AddressOf NucleicAcidStaticsProperty.ATPercent, lst.First, out, winSize, steps)
        End If

        Dim LQuery = (From genome As Integer
                      In lst.Sequence.AsParallel
                      Select genome,
                          percent = NucleicAcidStaticsProperty.ATPercent(lst(genome), winSize, steps, True)
                      Order By genome Ascending).ToArray
        Dim vector As Double() = __vectorCommon(LQuery.ToArray(Function(genome) genome.percent))
        Return vector.ToArray(Function(n) CStr(n)).FlushAllLines(out).CLICode
    End Function

    Private Function __propertyVector(method As NtProperty, inFasta As FastaToken, out As String, winSize As Integer, steps As Integer) As Integer
        Dim vector As Double() = method(inFasta, winSize, steps, True)
        Return vector.ToArray(Function(n) CStr(n)).FlushAllLines(out).CLICode
    End Function

    ''' <summary>
    ''' 如果只有一条序列，则只做一条序列的GCSkew，反之做所有序列的GCSkew的平均值，要求都是经过多序列比对对齐了的，默认第一条序列为参考序列
    ''' </summary>
    ''' <param name="args"></param>
    ''' <returns></returns>
    <ExportAPI("--GC.Skew", Usage:="--GC.Skew /in <in.fasta> [/win_size <200> /step <25> /out <out.txt>]")>
    Public Function GCSkew(args As CommandLine.CommandLine) As Integer
        Dim inFasta = FastaFile.Read(args("/in"))
        Dim winSize As Integer = args.GetValue("/win_size", 200)
        Dim steps As Integer = args.GetValue("/step", 25)
        Dim out As String = args.GetValue("/out", inFasta.FilePath.TrimFileExt & ".GCSkew.txt")

        If inFasta.NumberOfFasta = 1 Then
            Return __propertyVector(AddressOf NucleicAcidStaticsProperty.GCSkew, inFasta.First, out, winSize, steps)
        End If

        Dim LQuery = (From genome As Integer
                      In inFasta.Sequence.AsParallel
                      Select genome,
                          skew = LANS.SystemsBiology.SequenceModel.NucleotideModels.GCSkew(inFasta(genome), winSize, steps, True)
                      Order By genome Ascending).ToArray  ' 排序是因为可能没有做多序列比对对齐，在这里需要使用第一条序列的长度作为参考
        Dim vector As Double() = __vectorCommon(LQuery.ToArray(Function(genome) genome.skew))
        Return vector.ToArray(Function(n) CStr(n)).FlushAllLines(out).CLICode
    End Function

    Private Function __vectorCommon(vectors As Double()()) As Double()
        Dim LQuery As Double() = vectors(Scan0).ToArray(
            Function(null, idx) vectors.ToArray(Function(genome) genome(idx)).Average)
        Return LQuery
    End Function

    <ExportAPI("/MGA2Myva", Usage:="/MGA2Myva /in <mga_cog.csv> [/out <myva_cog.csv> /map <genome.gb>]")>
    Public Function MGA2Myva(args As CommandLine.CommandLine) As Integer
        Dim inFile As String = args("/in")
        Dim out As String = args.GetValue("/out", inFile.TrimFileExt & ".MyvaCOG.csv")
        Dim doc As MGACOG() = MGACOG.LoadDoc(inFile)
        doc = (From x In doc Select x.InvokeSet(NameOf(x.QueryName), x.QueryName.Split("|"c)(6))).ToArray
        Dim myva = MGACOG.ToMyvaCOG(doc)

        If Not String.IsNullOrEmpty(args("/map")) Then
            Dim gb = GBFF.File.Load(args("/map"))
            Dim maps As Dictionary(Of String, String) = gb.LocusMaps

            For Each x As MyvaCOG In myva
                If Not maps.ContainsKey(x.QueryName) Then
                    Continue For
                End If
                x.QueryName = maps(x.QueryName)
            Next
        End If

        Return myva.SaveTo(out).CLICode
    End Function

    <ExportAPI("/Alignment.Dumps", Usage:="/Alignment.Dumps /in <inDIR> [/out <out.Xml>]")>
    Public Function AlignmentTableDump(args As CommandLine.CommandLine) As Integer
        Dim inDIR As String = args("/in")
        Dim out As String = args.GetValue("/out", inDIR & ".Xml")
        Dim tbl As AlignmentTable = ParserAPI.CreateFromBlastn(inDIR)
        Return tbl.Save(out).CLICode
    End Function

    Public Function bg() As Integer
        Dim doc = CircosAPI.CreateDoc
        doc.chromosomes_units = "10000"
        Dim fa = New FastaToken("F:\2015.12.26.vir_genome_sequencing\genome_annotations\1329830.5.ED\1329830.5.ED.fna")
        Call CircosAPI.SetBasicProperty(doc, fa)

        Dim ptt = LoadPTT("F:\2015.12.26.vir_genome_sequencing\genome_annotations\1329830.5.ED\1329830.5.ED.ptt")

        Dim regulons = "F:\2015.12.26.vir_genome_sequencing\genome_annotations\1329830.5.ED\circos\Regulators.csv".LoadCsv(Of LocusLabels.Name).ToArray(Function(x) x.ToMeta)
        Dim resistss = "F:\2015.12.26.vir_genome_sequencing\genome_annotations\1329830.5.ED\circos\resistance.csv".LoadCsv(Of LocusLabels.Name)
        regulons.Add(resistss.ToArray(Function(x) x.ToMeta))
        regulons = HighLightsMeta.Distinct(regulons)

        Dim labels = New HighlightLabel(regulons)

        Call Circos.CircosAPI.AddPlotElement(doc, New Plots.TextLabel(labels))

        Dim regulations = "F:\2015.12.26.vir_genome_sequencing\genome_annotations\1329830.5.ED\MAST\Regulations.csv".LoadCsv(Of LANS.SystemsBiology.AnalysisTools.NBCR.Extensions.MEME_Suite.Analysis.GenomeMotifFootPrints.PredictedRegulationFootprint)
        Dim connector = FromVirtualFootprint(regulations, ptt, resistss)

        Call Circos.CircosAPI.AddPlotElement(doc, New Circos.Documents.Configurations.Nodes.Plots.Connector(connector))



        Dim cog = "F:\2015.12.26.vir_genome_sequencing\genome_annotations\1329830.5.ED\circos\output.MyvaCOG.csv".LoadCsv(Of MyvaCOG)
        Dim gb = LANS.SystemsBiology.Assembly.NCBI.GenBank.GBFF.File.Load("F:\2015.12.26.vir_genome_sequencing\genome_annotations\1329830.5.ED\1329830.5.ED.gb")
        doc = Circos.CircosAPI.GenerateGeneElements(doc, gb, cog, splitOverlaps:=False)
        Dim tbl = "F:\2015.12.26.vir_genome_sequencing\genome_annotations\1329830.5.ED\circos\1329830.5.ED.Blastn.Xml".LoadXml(Of AlignmentTable)
        Dim iddd = (From x In tbl.Hits Select x.Identity).ToArray
        Dim tblColor = CircosAPI.IdentityColors(iddd.Min, iddd.Max, 512)
        doc = CircosAPI.GenerateBlastnAlignment(doc, tbl, 1, 0.2, tblColor)

        Dim at = IO.File.ReadAllLines("F:\2015.12.26.vir_genome_sequencing\genome_annotations\1329830.5.ED\circos\1329830.5.2.ATPercent.txt").ToArray(Function(x) Val(x))
        Dim gc = IO.File.ReadAllLines("F:\2015.12.26.vir_genome_sequencing\genome_annotations\1329830.5.ED\circos\1329830.5.2.GCSkew.txt").ToArray(Function(x) Val(x))

        Dim repeats = "F:\2015.12.26.vir_genome_sequencing\genome_annotations\1329830.5.ED\circos\repeat.csv".LoadCsv(Of Circos.Repeat)

        Dim nnnt = "F:\2015.12.26.vir_genome_sequencing\genome_annotations\1329830.5.ED\circos\1329830.5.ED.Full_AT.txt".LoadDblArray

        Dim rMaps = New Circos.Documents.Karyotype.Highlights.Repeat(repeats, nnnt)

        Call Circos.CircosAPI.AddPlotElement(doc, New Plots.HighLight(rMaps))
        Call Circos.CircosAPI.AddPlotElement(doc, New Plots.Histogram(New NtProps.GCSkew(fa, 25, 250, True)))
        Call Circos.CircosAPI.AddPlotElement(doc, New Plots.Histogram(New NtProps.GCSkew(fa, 25, 250, True)))

        Dim ideo = doc.GetIdeogram

        Call Circos.CircosAPI.SetIdeogramWidth(ideo, 1)
        Call Circos.CircosAPI.SetIdeogramRadius(ideo, 0.25)

        Call CircosAPI.WriteData(doc, "F:\2015.12.26.vir_genome_sequencing\genome_annotations\1329830.5.ED\circos", False)
    End Function

    Public Class tRNA
        <Column("#seq-name")> Public Property seqName As String
        Public Property start As Integer
        Public Property [end] As Integer
        Public Property strand As String
        <Column("tRNA-type")> Public Property tRNAType As String
        <Column("Anti-Codon")> Public Property AntiCodon As String
    End Class

End Module