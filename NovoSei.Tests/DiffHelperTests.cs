using System.Collections.Generic;
using NovoSei.Core.Services;
using Xunit;

namespace NovoSei.Tests;

public class DiffHelperTests
{
    [Fact]
    public void GenerateDiff_ComTextosIdenticos_DeveRetornarTudoUnchanged()
    {
        // Arrange
        string texto = "Linha 1\nLinha 2\nLinha 3";

        // Act
        var diff = DiffHelper.GenerateDiff(texto, texto);

        // Assert
        Assert.Equal(3, diff.Count);
        Assert.All(diff, line => Assert.Equal(DiffType.Unchanged, line.Type));
        Assert.Equal("Linha 1", diff[0].Text);
        Assert.Equal("Linha 2", diff[1].Text);
        Assert.Equal("Linha 3", diff[2].Text);
    }

    [Fact]
    public void GenerateDiff_ComInsercao_DeveIdentificarInsercao()
    {
        // Arrange
        string antigo = "Linha 1\nLinha 2";
        string novo = "Linha 1\nLinha Inserida\nLinha 2";

        // Act
        var diff = DiffHelper.GenerateDiff(antigo, novo);

        // Assert
        Assert.Equal(3, diff.Count);
        Assert.Equal(DiffType.Unchanged, diff[0].Type);
        Assert.Equal(DiffType.Inserted, diff[1].Type);
        Assert.Equal("Linha Inserida", diff[1].Text);
        Assert.Equal(DiffType.Unchanged, diff[2].Type);
    }

    [Fact]
    public void GenerateDiff_ComDelecao_DeveIdentificarDelecao()
    {
        // Arrange
        string antigo = "Linha 1\nLinha Deletada\nLinha 2";
        string novo = "Linha 1\nLinha 2";

        // Act
        var diff = DiffHelper.GenerateDiff(antigo, novo);

        // Assert
        Assert.Equal(3, diff.Count);
        Assert.Equal(DiffType.Unchanged, diff[0].Type);
        Assert.Equal(DiffType.Deleted, diff[1].Type);
        Assert.Equal("Linha Deletada", diff[1].Text);
        Assert.Equal(DiffType.Unchanged, diff[2].Type);
    }

    [Fact]
    public void GenerateDiff_ComMudancaMista_DeveIdentificarAmbos()
    {
        // Arrange
        string antigo = "Linha A\nLinha B\nLinha C";
        string novo = "Linha A\nLinha B Alterada\nLinha C\nLinha D";

        // Act
        var diff = DiffHelper.GenerateDiff(antigo, novo);

        // Assert
        // A alteração de uma linha é representada por uma deleção seguida de uma inserção (ou vice-versa)
        Assert.Contains(diff, line => line.Type == DiffType.Deleted && line.Text == "Linha B");
        Assert.Contains(diff, line => line.Type == DiffType.Inserted && line.Text == "Linha B Alterada");
        Assert.Contains(diff, line => line.Type == DiffType.Inserted && line.Text == "Linha D");
        Assert.Equal("Linha A", diff[0].Text);
        Assert.Equal(DiffType.Unchanged, diff[0].Type);
    }
}
