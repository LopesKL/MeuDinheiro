import { Table, Button, Space } from 'antd';
import { useState, useEffect, useImperativeHandle, forwardRef } from 'react';
import { LoadingSpinner } from '@components/UI';

/**
 * Componente de tabela paginada com busca no backend
 * @param {Object} props
 * @param {Function} props.fetchData - Função para buscar dados (page, pageSize, sorterField, sortOrder)
 * @param {Array} props.columns - Colunas da tabela
 * @param {Array} props.actions - Ações por linha
 * @param {string} props.rowKey - Chave única das linhas (default: 'id')
 * @param {number} props.initialPageSize - Tamanho inicial da página (default: 5)
 * @param {Object} props.rowSelection - Configuração de seleção
 * @param {Object} props.expandable - Configuração de linhas expansíveis
 */
const PaginatedTable = forwardRef(({
  fetchData,
  columns = [],
  actions = [],
  rowKey = 'id',
  initialPageSize = 5,
  rowSelection,
  expandable,
  ...tableProps
}, ref) => {
  const [data, setData] = useState([]);
  const [loading, setLoading] = useState(false);
  const [pagination, setPagination] = useState({
    current: 1,
    pageSize: initialPageSize,
    total: 0,
  });
  const [sorter, setSorter] = useState({ field: null, order: null });

  const loadData = async (page = 1, pageSize = initialPageSize, sorterField = null, sortOrder = null) => {
    if (!fetchData) return;

    setLoading(true);
    try {
      const result = await fetchData(page, pageSize, sorterField, sortOrder);
      
      if (result && result.data) {
        setData(result.data);
        setPagination({
          current: page,
          pageSize,
          total: result.total || result.data.length,
        });
      }
    } catch (error) {
      console.error('Erro ao carregar dados:', error);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    loadData(1, initialPageSize);
  }, []);

  useImperativeHandle(ref, () => ({
    reload: () => {
      loadData(pagination.current, pagination.pageSize, sorter.field, sorter.order);
    },
    refresh: () => {
      loadData(1, initialPageSize);
    },
  }));

  const handleTableChange = (newPagination, filters, newSorter) => {
    const { current, pageSize } = newPagination;
    const sorterField = newSorter.field;
    const sortOrder = newSorter.order === 'ascend' ? 'asc' : newSorter.order === 'descend' ? 'desc' : null;

    setSorter({ field: sorterField, order: sortOrder });
    loadData(current, pageSize, sorterField, sortOrder);
  };

  const columnsWithActions = [...columns];
  
  if (actions.length > 0) {
    columnsWithActions.push({
      title: 'Ações',
      key: 'actions',
      fixed: 'right',
      width: 120,
      render: (_, record) => (
        <Space size="small">
          {actions.map((action, index) => (
            <Button
              key={index}
              type={action.type || 'link'}
              size="small"
              icon={action.icon}
              onClick={() => action.onClick(record)}
              danger={action.danger}
            >
              {action.label}
            </Button>
          ))}
        </Space>
      ),
    });
  }

  if (loading && data.length === 0) {
    return <LoadingSpinner />;
  }

  return (
    <Table
      columns={columnsWithActions}
      dataSource={data}
      rowKey={rowKey}
      loading={loading}
      pagination={{
        ...pagination,
        showSizeChanger: true,
        showTotal: (total) => `Total: ${total}`,
        pageSizeOptions: ['5', '10', '20', '50', '100'],
      }}
      onChange={handleTableChange}
      rowSelection={rowSelection}
      expandable={expandable}
      scroll={{ x: 'max-content' }}
      {...tableProps}
    />
  );
});

PaginatedTable.displayName = 'PaginatedTable';

export default PaginatedTable;
