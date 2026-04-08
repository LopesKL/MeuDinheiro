import { Form, Row, Col } from 'antd';
import { forwardRef, useImperativeHandle } from 'react';
import * as Inputs from '@components/inputs';

/**
 * Componente de formulário dinâmico baseado em configuração JSON
 * @param {Object} props
 * @param {Array} props.formConfig - Configuração do formulário
 * @param {Function} props.onSubmit - Função chamada ao submeter
 * @param {Object} props.initialValues - Valores iniciais
 * @param {Object} props.formProps - Props adicionais para o Form
 */
const DynamicForm = forwardRef(({ formConfig = [], onSubmit, initialValues, formProps = {} }, ref) => {
  const [form] = Form.useForm();

  useImperativeHandle(ref, () => ({
    submit: () => form.submit(),
    resetFields: () => form.resetFields(),
    getFieldsValue: () => form.getFieldsValue(),
    setFieldsValue: (values) => form.setFieldsValue(values),
  }));

  const renderInput = (question) => {
    const { type, id, label, placeholder, required, tooltip, help, ...inputProps } = question;

    const rules = [];
    if (required) {
      rules.push({ required: true, message: `${label} é obrigatório` });
    }

    const commonProps = {
      name: id,
      label,
      placeholder,
      rules,
      tooltip,
      help,
      ...inputProps,
    };

    switch (type) {
      case 'text':
        return <Inputs.TextInput {...commonProps} />;
      case 'textarea':
        return <Inputs.TextAreaInput {...commonProps} />;
      case 'email':
        return <Inputs.EmailInput {...commonProps} />;
      case 'password':
        return <Inputs.PasswordInput {...commonProps} />;
      case 'integer':
      case 'number':
        return <Inputs.NumberInput {...commonProps} />;
      case 'decimal':
      case 'currency':
        return <Inputs.CurrencyInput {...commonProps} />;
      case 'date':
        return <Inputs.DateInput {...commonProps} />;
      case 'datetime':
        return <Inputs.DateInput {...commonProps} showTime />;
      case 'time':
        return <Inputs.TimeInput {...commonProps} />;
      case 'range-date':
        return <Inputs.DateRangeInput {...commonProps} />;
      case 'select':
        return <Inputs.SelectInput {...commonProps} />;
      case 'multiselect':
        return <Inputs.SelectInput {...commonProps} mode="multiple" />;
      case 'tree-select':
        return <Inputs.TreeSelectInput {...commonProps} />;
      case 'phone':
        return <Inputs.PhoneInput {...commonProps} />;
      case 'cpf':
        return <Inputs.CpfInput {...commonProps} />;
      case 'cnpj':
        return <Inputs.CnpjInput {...commonProps} />;
      case 'cep':
        return <Inputs.CepInput {...commonProps} />;
      case 'checkbox':
        return <Inputs.CheckboxInput {...commonProps} />;
      case 'checkbox-group':
        return <Inputs.CheckboxGroupInput {...commonProps} />;
      case 'radio':
        return <Inputs.RadioInput {...commonProps} />;
      case 'switch':
        return <Inputs.SwitchInput {...commonProps} />;
      case 'toggle-switch':
        return <Inputs.ToggleSwitch {...commonProps} />;
      case 'conditional-switch':
        return <Inputs.ConditionalSwitch {...commonProps} />;
      case 'switch-group':
        return <Inputs.SwitchGroup {...commonProps} />;
      case 'images':
      case 'files':
        return <Inputs.FileUploadInput {...commonProps} />;
      default:
        return <Inputs.TextInput {...commonProps} />;
    }
  };

  const handleSubmit = (values) => {
    if (onSubmit) {
      onSubmit(values);
    }
  };

  return (
    <Form
      form={form}
      layout="vertical"
      onFinish={handleSubmit}
      initialValues={initialValues}
      {...formProps}
    >
      {formConfig.map((section, sectionIndex) => (
        <Row key={sectionIndex} gutter={16}>
          {section.questions?.map((question, questionIndex) => {
            const span = 24 / (section.columns || 1);
            return (
              <Col key={questionIndex} xs={24} sm={24} md={span} lg={span} xl={span}>
                {renderInput(question)}
              </Col>
            );
          })}
        </Row>
      ))}
    </Form>
  );
});

DynamicForm.displayName = 'DynamicForm';

export default DynamicForm;
