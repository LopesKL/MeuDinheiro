import { useState } from 'react';
import { Card, Form, InputNumber, Button, Typography, Table, Divider, Tabs } from 'antd';
import { financeApi } from '@services/financeApi';
import { App } from 'antd';

const { Title, Text } = Typography;

const formatMoney = (v) =>
  new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' }).format(Number(v || 0));

const Simulation = () => {
  const { message } = App.useApp();
  const [sandboxForm] = Form.useForm();
  const [meForm] = Form.useForm();
  const [sandboxRows, setSandboxRows] = useState([]);
  const [meRows, setMeRows] = useState([]);
  const [loading, setLoading] = useState(false);

  const runSandbox = async (values) => {
    setLoading(true);
    try {
      const res = await financeApi.projectionSandbox({
        monthlyIncomeAverage: values.monthlyIncomeAverage ?? 0,
        monthlyFixedExpenses: values.monthlyFixedExpenses ?? 0,
        monthlyVariableExpensesAverage: values.monthlyVariableExpensesAverage ?? 0,
        outstandingInstallmentsTotal: values.outstandingInstallmentsTotal ?? 0,
        debtMonthlyPayments: values.debtMonthlyPayments ?? 0,
        currentLiquidPatrimony: values.currentLiquidPatrimony ?? 0,
        monthsAhead: values.monthsAhead ?? 12,
      });
      setSandboxRows(
        (res?.months || []).map((m, i) => ({
          key: i,
          label: m.label,
          balance: m.projectedBalance,
          cumInc: m.cumulativeIncome,
          cumExp: m.cumulativeExpense,
        }))
      );
      message.success('Simulação atualizada (sandbox — não salva dados).');
    } catch (e) {
      message.error(e.message || 'Erro na simulação');
    } finally {
      setLoading(false);
    }
  };

  const runMe = async (values) => {
    setLoading(true);
    try {
      const res = await financeApi.projectionMe(values.monthsAhead ?? 12);
      setMeRows(
        (res?.months || []).map((m, i) => ({
          key: i,
          label: m.label,
          balance: m.projectedBalance,
          cumInc: m.cumulativeIncome,
          cumExp: m.cumulativeExpense,
        }))
      );
    } catch (e) {
      message.error(e.message || 'Erro na projeção');
    } finally {
      setLoading(false);
    }
  };

  const cols = [
    { title: 'Mês', dataIndex: 'label', key: 'label' },
    { title: 'Saldo projetado', dataIndex: 'balance', key: 'balance', render: formatMoney },
    { title: 'Renda acum.', dataIndex: 'cumInc', key: 'cumInc', render: formatMoney },
    { title: 'Despesa acum.', dataIndex: 'cumExp', key: 'cumExp', render: formatMoney },
  ];

  return (
    <div>
      <Title level={3} style={{ marginTop: 0 }}>
        Projeções
      </Title>
      <Tabs
        items={[
          {
            key: 'sandbox',
            label: 'Sandbox (sem login nos dados)',
            children: (
              <Card>
                <Text type="secondary">
                  Ajuste parâmetros livremente. O endpoint <code>/api/Projections/sandbox</code> não persiste nada.
                </Text>
                <Divider />
                <Form
                  form={sandboxForm}
                  layout="vertical"
                  onFinish={runSandbox}
                  initialValues={{
                    monthlyIncomeAverage: 5000,
                    monthlyFixedExpenses: 2000,
                    monthlyVariableExpensesAverage: 800,
                    outstandingInstallmentsTotal: 3000,
                    debtMonthlyPayments: 400,
                    currentLiquidPatrimony: 10000,
                    monthsAhead: 12,
                  }}
                >
                  <Form.Item name="monthlyIncomeAverage" label="Média renda mensal">
                    <InputNumber min={0} style={{ width: '100%' }} />
                  </Form.Item>
                  <Form.Item name="monthlyFixedExpenses" label="Gastos fixos mensais">
                    <InputNumber min={0} style={{ width: '100%' }} />
                  </Form.Item>
                  <Form.Item name="monthlyVariableExpensesAverage" label="Variáveis (média)">
                    <InputNumber min={0} style={{ width: '100%' }} />
                  </Form.Item>
                  <Form.Item name="outstandingInstallmentsTotal" label="Total parcelas em aberto (simulado)">
                    <InputNumber min={0} style={{ width: '100%' }} />
                  </Form.Item>
                  <Form.Item name="debtMonthlyPayments" label="Pagamentos mensais de dívidas">
                    <InputNumber min={0} style={{ width: '100%' }} />
                  </Form.Item>
                  <Form.Item name="currentLiquidPatrimony" label="Patrimônio líquido atual">
                    <InputNumber min={0} style={{ width: '100%' }} />
                  </Form.Item>
                  <Form.Item name="monthsAhead" label="Meses à frente">
                    <InputNumber min={1} max={60} style={{ width: '100%' }} />
                  </Form.Item>
                  <Button type="primary" htmlType="submit" loading={loading}>
                    Calcular
                  </Button>
                </Form>
                <Table style={{ marginTop: 16 }} columns={cols} dataSource={sandboxRows} pagination={{ pageSize: 6 }} scroll={{ x: true }} />
              </Card>
            ),
          },
          {
            key: 'me',
            label: 'Com meus dados (autenticado)',
            children: (
              <Card>
                <Form form={meForm} layout="inline" onFinish={runMe} initialValues={{ monthsAhead: 12 }}>
                  <Form.Item name="monthsAhead" label="Meses">
                    <InputNumber min={1} max={60} />
                  </Form.Item>
                  <Form.Item>
                    <Button type="primary" htmlType="submit" loading={loading}>
                      Projetar
                    </Button>
                  </Form.Item>
                </Form>
                <Table style={{ marginTop: 16 }} columns={cols} dataSource={meRows} pagination={{ pageSize: 6 }} scroll={{ x: true }} />
              </Card>
            ),
          },
        ]}
      />
    </div>
  );
};

export default Simulation;
