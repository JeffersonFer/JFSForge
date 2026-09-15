using JFSForge.Core.Domain;

namespace JFSForge.Core.Application
{
    public interface IElementoFactory
    {
        IElementoAlvenaria? Criar(object elementoBruto);
    }
}
