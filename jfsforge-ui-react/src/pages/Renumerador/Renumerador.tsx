import { useEffect, useState } from "react";
import { Button } from "../../components/Button/Button";
import { TextInput } from "../../components/TextInput/TextInput";
import styles from "./Renumerador.module.css";

export function Renumerador() {
  const [prefixo, setPrefixo] = useState("PAR");
  const [valorInicial, setValorInicial] = useState("1");
  const [quantidadeSelecionada, setQuantidadeSelecionada] = useState(0);

  useEffect(() => {
    const handler = (event: MessageEvent) => {
      const mensagem = event.data;

      if (mensagem.tipo === "selecaoConcluida") {
        setQuantidadeSelecionada(mensagem.quantidade);
      } else if (mensagem.tipo === "selecaoCancelada") {
        setQuantidadeSelecionada(0);
      }
    };

    window.chrome?.webview?.addEventListener("message", handler);

    return () => {
      window.chrome?.webview?.removeEventListener("message", handler);
    };
  }, []);

  const camposPreenchidos =
    prefixo.trim() !== "" && valorInicial.trim() !== "" && quantidadeSelecionada > 0;

  const handleSelecionarElevacoes = () => {
    window.chrome?.webview?.postMessage({ tipo: "selecionar" });
  };

  const handleProcessar = () => {
    window.chrome?.webview?.postMessage({
      tipo: "processar",
      prefixo,
      valorInicial,
    });
  };

  return (
    <div className={styles.container}>
      <h1 className={styles.titulo}>Renumerador de Elevações</h1>

      <Button onClick={handleSelecionarElevacoes}>
        {quantidadeSelecionada > 0
          ? `${quantidadeSelecionada} elevação(ões) selecionada(s)`
          : "Selecionar Elevações"}
      </Button>

      <div className={styles.campos}>
        <TextInput
          label="Prefixo"
          value={prefixo}
          onChange={setPrefixo}
          placeholder="PAR"
        />
        <TextInput
          label="Valor Inicial"
          value={valorInicial}
          onChange={setValorInicial}
        />
      </div>

      <Button onClick={handleProcessar} disabled={!camposPreenchidos}>
        Processar
      </Button>
    </div>
  );
}