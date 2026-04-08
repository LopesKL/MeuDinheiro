import { Switch } from 'antd';

/**
 * Componente base de grupo de switches
 */
const BaseSwitchGroupInput = ({ options = [], ...props }) => {
  return (
    <div>
      {options.map((option) => (
        <div key={option.value} style={{ marginBottom: 8 }}>
          <Switch {...props} checked={option.checked} onChange={option.onChange} />
          <span style={{ marginLeft: 8 }}>{option.label}</span>
        </div>
      ))}
    </div>
  );
};

export default BaseSwitchGroupInput;
