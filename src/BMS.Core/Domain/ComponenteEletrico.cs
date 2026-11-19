namespace BMS.Core.Domain;

/// <summary>
/// Base abstract class for all monitored electrical components in the BMS.
/// </summary>
public abstract class ComponenteEletrico
{
    public string Id { get; }
    public string Nome { get; }
    public bool Ativo { get; protected set; } = true;

    protected ComponenteEletrico(string id, string nome)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("ID do componente não pode ser vazio.", nameof(id));
        if (string.IsNullOrWhiteSpace(nome))
            throw new ArgumentException("Nome do componente não pode ser vazio.", nameof(nome));

        Id = id;
        Nome = nome;
    }

    public abstract string RealizarAutodiagnostico();
}
