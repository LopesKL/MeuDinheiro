import { TimePicker } from 'antd';
import dayjs from 'dayjs';
import 'dayjs/locale/pt-br';
import locale from 'antd/es/date-picker/locale/pt_BR';

dayjs.locale('pt-br');

/**
 * Componente base de input de hora usando Ant Design
 */
const BaseTimeInput = ({ ...props }) => {
  return <TimePicker {...props} locale={locale} style={{ width: '100%' }} />;
};

export default BaseTimeInput;
