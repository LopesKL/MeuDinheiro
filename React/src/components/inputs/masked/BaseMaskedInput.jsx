import { useState, useEffect, useRef } from 'react';
import { Input } from 'antd';

/**
 * Função para aplicar máscara ao valor
 */
const applyMask = (value, mask, maskChar = '_') => {
  if (!value) return '';
  
  const cleanValue = value.replace(/[^\d]/g, '');
  if (!cleanValue) return '';
  
  let maskedValue = '';
  let valueIndex = 0;
  
  for (let i = 0; i < mask.length; i++) {
    if (valueIndex >= cleanValue.length) {
      break;
    }
    
    if (mask[i] === '9') {
      maskedValue += cleanValue[valueIndex];
      valueIndex++;
    } else {
      maskedValue += mask[i];
    }
  }
  
  return maskedValue;
};

/**
 * Componente base de input mascarado compatível com React 19
 */
const BaseMaskedInput = ({ mask, maskChar = '_', value, onChange, onBlur, ...props }) => {
  const [maskedValue, setMaskedValue] = useState(() => {
    return value ? applyMask(String(value), mask, maskChar) : '';
  });
  const inputRef = useRef(null);
  const cursorPositionRef = useRef(null);

  useEffect(() => {
    if (value !== undefined) {
      const newMasked = applyMask(String(value || ''), mask, maskChar);
      if (newMasked !== maskedValue) {
        setMaskedValue(newMasked);
      }
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [value, mask, maskChar]);

  const handleChange = (e) => {
    const inputValue = e.target.value;
    const cursorPos = e.target.selectionStart;
    
    // Aplica a máscara
    const newMasked = applyMask(inputValue, mask, maskChar);
    
    // Se o valor não mudou (apenas caracteres não numéricos foram digitados), não faz nada
    if (newMasked === maskedValue && inputValue !== maskedValue) {
      return;
    }
    
    setMaskedValue(newMasked);
    
    if (onChange) {
      onChange({
        ...e,
        target: {
          ...e.target,
          value: newMasked,
        },
      });
    }
    
    // Ajusta a posição do cursor
    // Calcula a posição baseada na diferença de tamanho
    const lengthDiff = newMasked.length - maskedValue.length;
    let newCursorPos = cursorPos;
    
    if (lengthDiff > 0) {
      // Se adicionou caracteres, move o cursor para frente
      // Encontra a próxima posição válida (após um dígito)
      newCursorPos = cursorPos + lengthDiff;
      // Garante que não ultrapasse o tamanho do valor
      newCursorPos = Math.min(newCursorPos, newMasked.length);
    } else if (lengthDiff < 0) {
      // Se removeu caracteres, mantém a posição relativa
      newCursorPos = Math.max(0, cursorPos + lengthDiff);
    }
    
    cursorPositionRef.current = newCursorPos;
    setTimeout(() => {
      if (inputRef.current) {
        // O Input do Ant Design expõe o elemento input através da propriedade input
        const inputElement = inputRef.current.input || inputRef.current;
        if (inputElement && typeof inputElement.setSelectionRange === 'function') {
          inputElement.setSelectionRange(
            cursorPositionRef.current,
            cursorPositionRef.current
          );
        }
      }
    }, 0);
  };

  const handleBlur = (e) => {
    if (onBlur) {
      onBlur(e);
    }
  };

  return (
    <Input
      {...props}
      ref={inputRef}
      value={maskedValue}
      onChange={handleChange}
      onBlur={handleBlur}
    />
  );
};

export default BaseMaskedInput;
