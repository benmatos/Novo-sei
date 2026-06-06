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
        });

        modelBuilder.Entity<Processo>(entity =>
        {
            entity.HasKey(p => p.Id);
            entity.HasIndex(p => p.NumeroSequencial).IsUnique();
            entity.Property(p => p.NumeroSequencial).HasMaxLength(50).IsRequired();
            entity.Property(p => p.Assunto).HasMaxLength(500).IsRequired();
            entity.Property(p => p.Status).HasMaxLength(30).IsRequired();
            entity.HasOne(p => p.Usuario)
                  .WithMany(u => u.Processos)
                  .HasForeignKey(p => p.UsuarioId)
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
    }
}
