import { Form } from 'antd';
import BaseSelectInput from './BaseSelectInput';

/**
 * Wrapper de select com integração ao Form do Ant Design
 */
const SelectInput = ({ name, label, rules, ...props }) => {
  return (
    <Form.Item name={name} label={label} rules={rules}>
      <BaseSelectInput {...props} />
    </Form.Item>
  );
};

export default SelectInput;
