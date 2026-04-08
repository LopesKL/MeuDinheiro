import { Form } from 'antd';
import BaseConditionalSwitchInput from './BaseConditionalSwitchInput';

/**
 * Wrapper de switch condicional com integração ao Form do Ant Design
 */
const ConditionalSwitch = ({ name, label, rules, ...props }) => {
  return (
    <Form.Item name={name} label={label} valuePropName="checked" rules={rules}>
      <BaseConditionalSwitchInput {...props} />
    </Form.Item>
  );
};

export default ConditionalSwitch;
