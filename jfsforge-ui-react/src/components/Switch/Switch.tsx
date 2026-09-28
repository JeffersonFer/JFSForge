import styles from "./Switch.module.css";

type SwitchProps = {
  label: string;
  ativo: boolean;
  onToggle: () => void;
};

export function Switch({ label, ativo, onToggle }: SwitchProps) {
  return (
    <label className={styles.container}>
      <span className={styles.label}>{label}</span>
      <button
        type="button"
        role="switch"
        aria-checked={ativo}
        className={`${styles.trilho} ${ativo ? styles.ativo : ""}`}
        onClick={onToggle}
      >
        <span className={styles.bolinha} />
      </button>
    </label>
  );
}