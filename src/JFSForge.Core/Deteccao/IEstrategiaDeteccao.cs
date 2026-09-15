using JFSForge.Core.Domain;

namespace JFSForge.Core.Deteccao
{
    public interface IEstrategiaDeteccao
    {
        IEnumerable<IElementoAlvenaria> Detectar(IElementoAlvenaria elementoAlvenaria);
    }
}
