import { Form } from 'antd';
import BaseRadioInput from './BaseRadioInput';

/**
 * Wrapper de radio group com integração ao Form do Ant Design
 */
const RadioInput = ({ name, label, rules, ...props }) => {
  return (
    <Form.Item name={name} label={label} rules={rules}>
      <BaseRadioInput {...props} />
    </Form.Item>
  );
};

export default RadioInput;
