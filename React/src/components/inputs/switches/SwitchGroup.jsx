import { Form } from 'antd';
import BaseSwitchGroupInput from './BaseSwitchGroupInput';

/**
 * Wrapper de grupo de switches com integração ao Form do Ant Design
 */
const SwitchGroup = ({ name, label, rules, ...props }) => {
  return (
    <Form.Item name={name} label={label} rules={rules}>
      <BaseSwitchGroupInput {...props} />
    </Form.Item>
  );
};

export default SwitchGroup;
