using Microsoft.EntityFrameworkCore;
using NovoSei.Core.Entities;

namespace NovoSei.Infrastructure.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Processo> Processos => Set<Processo>();
    public DbSet<Documento> Documentos => Set<Documento>();
    public DbSet<Assinatura> Assinaturas => Set<Assinatura>();
    public DbSet<TemplateDocumento> TemplatesDocumento => Set<TemplateDocumento>();
    public DbSet<Orgao> Orgaos => Set<Orgao>();
    public DbSet<Unidade> Unidades => Set<Unidade>();
    public DbSet<Marcador> Marcadores => Set<Marcador>();
    public DbSet<Comentario> Comentarios => Set<Comentario>();
    public DbSet<ControlePrazo> ControlePrazos => Set<ControlePrazo>();
    public DbSet<Feriado> Feriados => Set<Feriado>();
    public DbSet<BlocoReuniao> BlocosReuniao => Set<BlocoReuniao>();
    public DbSet<BlocoReuniaoUnidade> BlocoReuniaoUnidades => Set<BlocoReuniaoUnidade>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Usuario>(entity =>
        {
            entity.HasKey(u => u.Id);
            entity.HasIndex(u => u.Email).IsUnique();
            entity.HasIndex(u => u.Login).IsUnique();
            entity.Property(u => u.Nome).HasMaxLength(200).IsRequired();
            entity.Property(u => u.Email).HasMaxLength(300).IsRequired();
            entity.Property(u => u.Login).HasMaxLength(100).IsRequired();
            entity.Property(u => u.Perfil).HasMaxLength(50).IsRequired();
            entity.Property(u => u.Segredo2Fa).HasMaxLength(128);
            entity.Property(u => u.EmailAlternativo).HasMaxLength(300);
            entity.Property(u => u.Token2FaAtivacao).HasMaxLength(128);
        });

        modelBuilder.Entity<Processo>(entity =>
        {
            entity.HasKey(p => p.Id);
            entity.HasIndex(p => p.NumeroSequencial).IsUnique();
            entity.Property(p => p.NumeroSequencial).HasMaxLength(50).IsRequired();
            entity.Property(p => p.Assunto).HasMaxLength(500).IsRequired();
            entity.Property(p => p.Status).HasMaxLength(30).IsRequired();
            entity.Property(p => p.Tipo).HasMaxLength(100).IsRequired();
            entity.Property(p => p.Interessados).HasMaxLength(500);
            entity.Property(p => p.NivelAcesso).HasMaxLength(50).IsRequired();
            entity.HasOne(p => p.Usuario)
                  .WithMany(u => u.Processos)
                  .HasForeignKey(p => p.UsuarioId)
                  .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(p => p.Unidade)
                  .WithMany(u => u.Processos)
                  .HasForeignKey(p => p.UnidadeId)
                  .OnDelete(DeleteBehavior.Restrict);
            entity.ToTable(tb => tb.IsTemporal());
        });

        modelBuilder.Entity<Documento>(entity =>
        {
            entity.HasKey(d => d.Id);
            entity.Property(d => d.Titulo).HasMaxLength(300).IsRequired();
            entity.Property(d => d.Status).HasMaxLength(30).IsRequired();
            entity.Property(d => d.ConteudoHtml).HasColumnType("nvarchar(max)").IsRequired();
            entity.HasOne(d => d.Processo)
                  .WithMany(p => p.Documentos)
                  .HasForeignKey(d => d.ProcessoId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(d => d.TemplateDocumento)
                  .WithMany(t => t.Documentos)
                  .HasForeignKey(d => d.TemplateDocumentoId)
                  .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(d => d.Unidade)
                  .WithMany()
                  .HasForeignKey(d => d.UnidadeId)
                  .OnDelete(DeleteBehavior.Restrict);
            entity.ToTable(tb => tb.IsTemporal());
        });

        modelBuilder.Entity<Assinatura>(entity =>
        {
            entity.HasKey(a => a.Id);
            entity.Property(a => a.HashSha256).HasMaxLength(64).IsRequired();
            entity.HasOne(a => a.Documento)
                  .WithMany(d => d.Assinaturas)
                  .HasForeignKey(a => a.DocumentoId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(a => a.Usuario)
                  .WithMany(u => u.Assinaturas)
                  .HasForeignKey(a => a.UsuarioId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<TemplateDocumento>(entity =>
        {
            entity.HasKey(t => t.Id);
            entity.Property(t => t.Nome).HasMaxLength(200).IsRequired();
            entity.Property(t => t.ConteudoHtmlBase).HasColumnType("nvarchar(max)").IsRequired();
        });

        modelBuilder.Entity<Orgao>(entity =>
        {
            entity.HasKey(o => o.Id);
            entity.Property(o => o.Sigla).HasMaxLength(50).IsRequired();
            entity.Property(o => o.Descricao).HasMaxLength(250).IsRequired();
            entity.HasIndex(o => o.Sigla).IsUnique();
            entity.ToTable(tb => tb.IsTemporal());
        });

        modelBuilder.Entity<Unidade>(entity =>
        {
            entity.HasKey(u => u.Id);
            entity.Property(u => u.Sigla).HasMaxLength(50).IsRequired();
            entity.Property(u => u.Descricao).HasMaxLength(250).IsRequired();
            entity.HasIndex(u => new { u.OrgaoId, u.Sigla }).IsUnique();
            entity.HasOne(u => u.Orgao)
                  .WithMany(o => o.Unidades)
                  .HasForeignKey(u => u.OrgaoId)
                  .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(u => u.ParentUnidade)
                  .WithMany(u => u.ChildUnidades)
                  .HasForeignKey(u => u.ParentUnidadeId)
                  .OnDelete(DeleteBehavior.Restrict);
            entity.ToTable(tb => tb.IsTemporal());
        });

        modelBuilder.Entity<Usuario>()
            .HasMany(u => u.Unidades)
            .WithMany(u => u.Usuarios)
            .UsingEntity<Dictionary<string, object>>(
                "UsuariosUnidades",
                j => j.HasOne<Unidade>().WithMany().HasForeignKey("UnidadeId").OnDelete(DeleteBehavior.Cascade),
                j => j.HasOne<Usuario>().WithMany().HasForeignKey("UsuarioId").OnDelete(DeleteBehavior.Cascade)
            );

        modelBuilder.Entity<Marcador>(entity =>
        {
            entity.HasKey(m => m.Id);
            entity.Property(m => m.Nome).HasMaxLength(100).IsRequired();
            entity.Property(m => m.CorHex).HasMaxLength(7).IsRequired();
            entity.HasOne(m => m.Unidade)
                  .WithMany()
                  .HasForeignKey(m => m.UnidadeId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(m => m.Processos)
                  .WithMany(p => p.Marcadores)
                  .UsingEntity<Dictionary<string, object>>(
                      "ProcessosMarcadores",
                      j => j.HasOne<Processo>().WithMany().HasForeignKey("ProcessoId").OnDelete(DeleteBehavior.Cascade),
                      j => j.HasOne<Marcador>().WithMany().HasForeignKey("MarcadorId").OnDelete(DeleteBehavior.Cascade)
                  );
            entity.ToTable(tb => tb.IsTemporal());
        });

        modelBuilder.Entity<Comentario>(entity =>
        {
            entity.HasKey(c => c.Id);
            entity.Property(c => c.Descricao).HasMaxLength(1000).IsRequired();
            entity.HasOne(c => c.Processo)
                  .WithMany(p => p.Comentarios)
                  .HasForeignKey(c => c.ProcessoId)
                  .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(c => c.Documento)
                  .WithMany(d => d.Comentarios)
                  .HasForeignKey(c => c.DocumentoId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(c => c.Usuario)
                  .WithMany()
                  .HasForeignKey(c => c.UsuarioId)
                  .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(c => c.Unidade)
                  .WithMany()
                  .HasForeignKey(c => c.UnidadeId)
                  .OnDelete(DeleteBehavior.Restrict);
            entity.ToTable(tb => tb.IsTemporal());
        });

        modelBuilder.Entity<ControlePrazo>(entity =>
        {
            entity.HasKey(c => c.Id);
            entity.Property(c => c.Status).HasMaxLength(30).IsRequired();
            entity.HasOne(c => c.Processo)
                  .WithMany(p => p.ControlePrazos)
                  .HasForeignKey(c => c.ProcessoId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(c => c.Unidade)
                  .WithMany()
                  .HasForeignKey(c => c.UnidadeId)
                  .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(c => c.CriadoPorUsuario)
                  .WithMany()
                  .HasForeignKey(c => c.CriadoPorUsuarioId)
                  .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(c => c.ResolvidoPorUsuario)
                  .WithMany()
                  .HasForeignKey(c => c.ResolvidoPorUsuarioId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Feriado>(entity =>
        {
            entity.HasKey(f => f.Id);
            entity.HasIndex(f => f.Data).IsUnique();
            entity.Property(f => f.Descricao).HasMaxLength(200).IsRequired();
        });

        modelBuilder.Entity<BlocoReuniao>(entity =>
        {
            entity.HasKey(b => b.Id);
            entity.Property(b => b.Descricao).HasMaxLength(500).IsRequired();
            entity.Property(b => b.Status).HasMaxLength(30).IsRequired();
            entity.HasOne(b => b.GeradoraUnidade)
                  .WithMany()
                  .HasForeignKey(b => b.GeradoraUnidadeId)
                  .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(b => b.CriadoPorUsuario)
                  .WithMany()
                  .HasForeignKey(b => b.CriadoPorUsuarioId)
                  .OnDelete(DeleteBehavior.Restrict);
            entity.HasMany(b => b.Processos)
                  .WithMany()
                  .UsingEntity("BlocosReuniaoProcessos");
        });

        modelBuilder.Entity<BlocoReuniaoUnidade>(entity =>
        {
            entity.HasKey(bu => new { bu.BlocoReuniaoId, bu.UnidadeReceptoraId });
            entity.Property(bu => bu.Status).HasMaxLength(30).IsRequired();
            entity.HasOne(bu => bu.BlocoReuniao)
                  .WithMany(b => b.BlocoUnidades)
                  .HasForeignKey(bu => bu.BlocoReuniaoId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(bu => bu.UnidadeReceptora)
                  .WithMany()
                  .HasForeignKey(bu => bu.UnidadeReceptoraId)
                  .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
