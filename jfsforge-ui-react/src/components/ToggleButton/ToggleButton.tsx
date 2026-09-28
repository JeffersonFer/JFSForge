import styles from "./ToggleButton.module.css";

type ToggleButtonProps = {
  label: string;
  ativo: boolean;
  onToggle: () => void;
};

export function ToggleButton({ label, ativo, onToggle }: ToggleButtonProps) {
  return (
    <button
      type="button"
      className={`${styles.toggle} ${ativo ? styles.ativo : ""}`}
      onClick={onToggle}
    >
      {label}
    </button>
  );
}