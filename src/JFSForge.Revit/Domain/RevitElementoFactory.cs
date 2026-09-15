using Autodesk.Revit.DB;
using JFSForge.Core.Application;
using JFSForge.Core.Domain;
using JFSForge.Core.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JFSForge.Revit.Domain
{
    internal class RevitElementoFactory : IElementoFactory
    {
        public IElementoAlvenaria? Criar(object elementoBruto)
        {
            if (elementoBruto is not Element element)
                return null;

            if (RevitElementoIdentificador.IsParedeEstrutural(element))
                return new Parede(element.Id.ToString());

            if (RevitElementoIdentificador.IsBloco(element))
                return new Bloco(element.Id.ToString());

            if (RevitElementoIdentificador.IsGrauteHorizontal(element))
                return new GrauteHorizontal(element.Id.ToString());

            return null;
        }
    }
}
