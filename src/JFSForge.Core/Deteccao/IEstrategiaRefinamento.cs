using JFSForge.Core.Domain;

namespace JFSForge.Core.Deteccao
{
    public interface IEstrategiaRefinamento
    {
        IEnumerable<IElementoAlvenaria> Refinar(
        IElementoAlvenaria elementoAlvenaria,
        IEnumerable<IElementoAlvenaria> candidatos);
    }
}
