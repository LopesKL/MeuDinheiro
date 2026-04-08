import { DatePicker } from 'antd';
import dayjs from 'dayjs';
import 'dayjs/locale/pt-br';
import locale from 'antd/es/date-picker/locale/pt_BR';

dayjs.locale('pt-br');

/**
 * Componente base de input de range de datas usando Ant Design
 */
const BaseDateRangeInput = ({ ...props }) => {
  return <DatePicker.RangePicker {...props} locale={locale} style={{ width: '100%' }} />;
};

export default BaseDateRangeInput;
