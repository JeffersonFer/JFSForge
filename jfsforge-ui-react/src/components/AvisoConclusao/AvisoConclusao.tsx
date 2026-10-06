import styles from "./AvisoConclusao.module.css";

type AvisoConclusaoProps = {
  mensagem: string;
  tipo?: "sucesso" | "erro";
};

export function AvisoConclusao({ mensagem, tipo = "sucesso" }: AvisoConclusaoProps) {
  return (
    <div
      role="status"
      className={`${styles.aviso} ${tipo === "erro" ? styles.erro : ""}`}
    >
      <span className={styles.icone}>{tipo === "erro" ? "!" : "✓"}</span>
      <span>{mensagem}</span>
    </div>
  );
}