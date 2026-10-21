export default function stripIds(text: string): string {
  return text.replace(/\[\d+\]\s*/g, "");
}
