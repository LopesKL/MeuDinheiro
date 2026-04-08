import { Form } from 'antd';
import BaseMaskedInput from './BaseMaskedInput';

/**
 * Wrapper de input de CEP com máscara
 */
const CepInput = ({ name, label, rules, ...props }) => {
  return (
    <Form.Item name={name} label={label} rules={rules}>
      <BaseMaskedInput mask="99999-999" {...props} />
    </Form.Item>
  );
};

export default CepInput;
