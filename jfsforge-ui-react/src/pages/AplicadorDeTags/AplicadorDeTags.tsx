import { useState } from "react";
import { Card } from "../../components/Card/Card";
import { Switch } from "../../components/Switch/Switch";
import { Button } from "../../components/Button/Button";
import styles from "./AplicadorDeTags.module.css";

const TIPOS_DE_TAG = [
  { chave: "tagJanela", label: "Janela" },
  { chave: "tagPorta", label: "Porta" },
  { chave: "tagBarraHorizontal", label: "Aço Horizontal" },
  { chave: "tagViga", label: "Viga" },
  { chave: "tagPilar", label: "Pilar" },
] as const;

type ChaveTipoDeTag = (typeof TIPOS_DE_TAG)[number]["chave"];

const TIPOS_DE_VISTA = [
  { chave: "elevacoes", label: "Elevações" },
  { chave: "cortes", label: "Cortes" },
  { chave: "plantas", label: "Plantas" },
];

export function AplicadorDeTags() {
  const [tiposAtivos, setTiposAtivos] = useState<Record<ChaveTipoDeTag, boolean>>({
    tagJanela: false,
    tagPorta: false,
    tagBarraHorizontal: false,
    tagViga: false,
    tagPilar: false,
  });

  const algumTipoAtivo = Object.values(tiposAtivos).some((ativo) => ativo);

  const alternarTipo = (chave: ChaveTipoDeTag) => {
    setTiposAtivos((anterior) => ({
      ...anterior,
      [chave]: !anterior[chave],
    }));
  };

  const aplicarTags = (tipoDeVista: string, apenasVistaAtiva: boolean) => {
    window.chrome?.webview?.postMessage({
      tipo: "aplicar-tags",
      tipoDeVista,
      apenasVistaAtiva,
      ...tiposAtivos,
    });
  };

  return (
    <div className={styles.container}>
      <Card titulo="Tipos de Tag">
        {TIPOS_DE_TAG.map(({ chave, label }) => (
          <Switch
            key={chave}
            label={label}
            ativo={tiposAtivos[chave]}
            onToggle={() => alternarTipo(chave)}
          />
        ))}
      </Card>

      {TIPOS_DE_VISTA.map(({ chave, label }) => (
        <Card key={chave} titulo={label}>
          <Button onClick={() => aplicarTags(chave, true)} disabled={!algumTipoAtivo}>
            Vista Ativa
          </Button>
          <Button onClick={() => aplicarTags(chave, false)} disabled={!algumTipoAtivo}>
            Todas as Vistas
          </Button>
        </Card>
      ))}
    </div>
  );
}