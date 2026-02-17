import JsBarcode from 'jsbarcode';
import { useEffect, useRef } from 'react';

interface ScanBarcodeSvgProps {
  value: string;
  width?: number;
  height?: number;
  displayValue?: boolean;
}

export function ScanBarcodeSvg({
  value,
  width = 1.4,
  height = 36,
  displayValue = true,
}: ScanBarcodeSvgProps) {
  const svgRef = useRef<SVGSVGElement | null>(null);
  const normalizedValue = value.trim();

  useEffect(() => {
    if (!svgRef.current || normalizedValue.length === 0) {
      return;
    }

    JsBarcode(svgRef.current, normalizedValue, {
      format: 'CODE128',
      displayValue,
      width,
      height,
      fontSize: 12,
      margin: 4,
    });
  }, [displayValue, height, normalizedValue, width]);

  if (normalizedValue.length === 0) {
    return <span className="text-muted">-</span>;
  }

  return <svg ref={svgRef} />;
}
